// Container.cs (V2-only, multi-recipient + owner-signed header)
// - No password/PBKDF2/v1 support.
// - Data encryption: AES-GCM with random DEK (32 bytes).
// - Key distribution: DEK wrapped per recipient via RSA-OAEP-SHA256.
// - Tamper prevention for recipients/header: owner signs header via RSA-PSS-SHA256.
//   Any add/remove recipient requires owner private key to re-sign.
//   Load verifies header signature before unwrapping DEK.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace HSTRYDoc
{
    public sealed class HSTRYContainer
    {
        public const byte CurrentVersion = 2;

        private const byte RECIPIENT_ALG_RSA_OAEP_SHA256 = 1;
        private const byte HEADER_SIGALG_RSA_PSS_SHA256 = 1;

        public byte Version { get; private set; } = CurrentVersion;

        public string EncodingWebName { get; private set; } = Global.CurrentEditorEncoding.WebName;

        // Owner public key (SPKI) + header signature (prevents recipient list tampering)
        public byte[] OwnerPublicKeySpki { get; private set; } = Array.Empty<byte>();
        public byte[] OwnerKeyId => (OwnerPublicKeySpki.Length == 0) ? Array.Empty<byte>() : SHA256.HashData(OwnerPublicKeySpki);

        public byte HeaderSignatureAlg { get; private set; } = HEADER_SIGALG_RSA_PSS_SHA256;
        public byte[] HeaderSignature { get; private set; } = Array.Empty<byte>();

        private readonly List<RecipientEntry> _recipients = new();
        public IReadOnlyList<RecipientEntry> Recipients => _recipients;

        private readonly List<Block> _blocks = new();
        public IReadOnlyList<Block> Blocks => _blocks;

        // DEK held in memory while container is open (AES-256)
        private byte[] _key = Array.Empty<byte>();

        private HSTRYContainer() { }

        // ============================================================
        // Key files (private key as file)
        // ============================================================
        public static class RsaKeyFiles
        {
            // Public key file contains Base64(SPKI)
            public static void SavePublicKeySpki(string path, RSA rsa)
            {
                byte[] spki = rsa.ExportSubjectPublicKeyInfo();
                File.WriteAllText(path, Convert.ToBase64String(spki), Encoding.ASCII);
            }

            public static RSA LoadPublicKeySpki(string path)
            {
                string b64 = File.ReadAllText(path, Encoding.ASCII).Trim();
                byte[] spki = Convert.FromBase64String(b64);

                var rsa = RSA.Create();
                rsa.ImportSubjectPublicKeyInfo(spki, out _);
                return rsa;
            }

            // Private key file contains Base64(PKCS#8)
            public static void SavePrivateKeyPkcs8(string path, RSA rsa)
            {
                byte[] pkcs8 = rsa.ExportPkcs8PrivateKey();
                File.WriteAllText(path, Convert.ToBase64String(pkcs8), Encoding.ASCII);
            }

            public static RSA LoadPrivateKeyPkcs8(string path)
            {
                string b64 = File.ReadAllText(path, Encoding.ASCII).Trim();
                byte[] pkcs8 = Convert.FromBase64String(b64);

                var rsa = RSA.Create();
                rsa.ImportPkcs8PrivateKey(pkcs8, out _);
                return rsa;
            }

            // KeyId = SHA256(SPKI)
            public static byte[] ComputeKeyIdFromPublicKey(RSA rsa)
            {
                byte[] spki = rsa.ExportSubjectPublicKeyInfo();
                return SHA256.HashData(spki);
            }

            public static RSA CreateNewKeyPair(int bits = 3072)
            {
                var rsa = RSA.Create();
                rsa.KeySize = bits;
                return rsa;
            }
        }

        // ============================================================
        // Create / Load
        // ============================================================

        /// <summary>
        /// Create a new container (V2).
        /// - ownerPrivateKey signs the header.
        /// - recipients are the public keys that can open the container (should include owner).
        /// </summary>
        public static HSTRYContainer CreateNewForRecipients(
            RSA ownerPrivateKey,
            IEnumerable<RSA> recipientPublicKeys,
            Encoding? encoding = null)
        {
            if (ownerPrivateKey == null) throw new ArgumentNullException(nameof(ownerPrivateKey));
            if (recipientPublicKeys == null) throw new ArgumentNullException(nameof(recipientPublicKeys));

            var pubs = recipientPublicKeys.ToList();
            if (pubs.Count == 0)
                throw new ArgumentException("At least one recipient public key is required.");

            var c = new HSTRYContainer
            {
                Version = CurrentVersion
            };

            var enc = encoding ?? Global.CurrentEditorEncoding;
            c.EncodingWebName = enc.WebName;

            c.OwnerPublicKeySpki = ownerPrivateKey.ExportSubjectPublicKeyInfo();
            c.HeaderSignatureAlg = HEADER_SIGALG_RSA_PSS_SHA256;

            // Generate DEK (AES-256)
            c._key = new byte[32];
            RandomNumberGenerator.Fill(c._key);

            // Wrap DEK for each recipient
            foreach (var rsaPub in pubs)
            {
                byte[] keyId = RsaKeyFiles.ComputeKeyIdFromPublicKey(rsaPub);
                byte[] wrappedDek = rsaPub.Encrypt(c._key, RSAEncryptionPadding.OaepSHA256);

                c._recipients.Add(new RecipientEntry
                {
                    KeyId = keyId,
                    Alg = RECIPIENT_ALG_RSA_OAEP_SHA256,
                    WrappedDek = wrappedDek
                });
            }

            // Ensure unique KeyIds
            if (c._recipients.Select(r => Convert.ToHexString(r.KeyId)).Distinct(StringComparer.OrdinalIgnoreCase).Count() != c._recipients.Count)
                throw new InvalidOperationException("Duplicate recipient keys detected (KeyId collision).");

            // Sign header
            c.ResignHeader(ownerPrivateKey);

            return c;
        }

        public static HSTRYContainer LoadWithPrivateKeyFile(string containerPath, string privateKeyPath)
        {
            using var fs = File.OpenRead(containerPath);
            using var rsaPriv = RsaKeyFiles.LoadPrivateKeyPkcs8(privateKeyPath);
            return LoadWithPrivateKey(fs, rsaPriv);
        }

        public static HSTRYContainer LoadWithPrivateKey(Stream stream, RSA privateKey)
        {
            if (privateKey == null) throw new ArgumentNullException(nameof(privateKey));

            using var br = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

            byte[] magic = br.ReadBytes(Global.FileMagic.Length);
            if (!magic.SequenceEqual(Global.FileMagic))
                throw new InvalidDataException("Invalid file format (magic mismatch).");

            byte version = br.ReadByte();
            if (version != CurrentVersion)
                throw new InvalidDataException($"Unsupported container version: {version}.");

            var c = new HSTRYContainer { Version = version };

            // ----- Header (unsigned parts first) -----
            int encNameLen = br.ReadByte();
            c.EncodingWebName = Encoding.UTF8.GetString(br.ReadBytes(encNameLen));

            ushort ownerPubLen = br.ReadUInt16();
            c.OwnerPublicKeySpki = br.ReadBytes(ownerPubLen);

            int recipientCount = br.ReadInt32();
            if (recipientCount <= 0)
                throw new InvalidDataException("Recipient list is empty.");

            for (int i = 0; i < recipientCount; i++)
            {
                int keyIdLen = br.ReadByte();
                byte[] keyId = br.ReadBytes(keyIdLen);

                byte alg = br.ReadByte();

                ushort wrappedLen = br.ReadUInt16();
                byte[] wrappedDek = br.ReadBytes(wrappedLen);

                c._recipients.Add(new RecipientEntry
                {
                    KeyId = keyId,
                    Alg = alg,
                    WrappedDek = wrappedDek
                });
            }

            c.HeaderSignatureAlg = br.ReadByte();
            ushort sigLen = br.ReadUInt16();
            c.HeaderSignature = br.ReadBytes(sigLen);

            // Verify signature FIRST (prevents recipient/header tampering)
            c.VerifyHeaderSignatureOrThrow();

            // Find matching recipient by KeyId derived from this private key
            byte[] myKeyId = RsaKeyFiles.ComputeKeyIdFromPublicKey(privateKey);

            var entry = c._recipients.FirstOrDefault(r => r.KeyId.SequenceEqual(myKeyId));
            if (entry == null)
                throw new CryptographicException("No matching recipient entry for this private key.");

            if (entry.Alg != RECIPIENT_ALG_RSA_OAEP_SHA256)
                throw new CryptographicException("Unsupported recipient key algorithm.");

            // Unwrap DEK
            byte[] dek;
            try
            {
                dek = privateKey.Decrypt(entry.WrappedDek, RSAEncryptionPadding.OaepSHA256);
            }
            catch
            {
                throw new CryptographicException("Failed to decrypt DEK with the provided private key.");
            }

            if (dek == null || dek.Length != 32)
                throw new CryptographicException("Invalid DEK length.");

            c._key = dek;

            // ----- Blocks -----
            int blockCount = br.ReadInt32();
            for (int i = 0; i < blockCount; i++)
            {
                var b = new Block
                {
                    Index = br.ReadInt32()
                };

                ushort titleLen = br.ReadUInt16();
                b.Title = Encoding.UTF8.GetString(br.ReadBytes(titleLen));

                b.CreatedUtc = new DateTimeOffset(br.ReadInt64(), TimeSpan.Zero);
                b.ModifiedUtc = new DateTimeOffset(br.ReadInt64(), TimeSpan.Zero);

                b.PrevHash = br.ReadBytes(Crypto.Sha256Size);

                b.Nonce = br.ReadBytes(Crypto.AesGcmNonceSize);
                b.Tag = br.ReadBytes(Crypto.AesGcmTagSize);

                int ctLen = br.ReadInt32();
                b.Ciphertext = br.ReadBytes(ctLen);

                c._blocks.Add(b);
            }

            if (!c.Validate(out string error))
                throw new InvalidDataException($"Container invalid: {error}");

            return c;
        }

        // ============================================================
        // Save
        // ============================================================
        public void Save(string path)
        {
            using var fs = File.Create(path);
            Save(fs);
        }

        public void Save(Stream stream)
        {
            EnsureKey();
            EnsureHeaderSigned();

            using var bw = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

            bw.Write(Global.FileMagic);
            bw.Write(Version);

            byte[] encBytes = Encoding.UTF8.GetBytes(EncodingWebName);
            bw.Write((byte)encBytes.Length);
            bw.Write(encBytes);

            if (OwnerPublicKeySpki == null || OwnerPublicKeySpki.Length == 0)
                throw new InvalidDataException("Owner public key is missing.");
            if (OwnerPublicKeySpki.Length > ushort.MaxValue)
                throw new InvalidDataException("Owner public key is too large.");

            bw.Write((ushort)OwnerPublicKeySpki.Length);
            bw.Write(OwnerPublicKeySpki);

            bw.Write(_recipients.Count);
            foreach (var r in _recipients)
            {
                if (r.KeyId == null || r.KeyId.Length == 0)
                    throw new InvalidDataException("Recipient KeyId missing.");
                if (r.WrappedDek == null || r.WrappedDek.Length == 0)
                    throw new InvalidDataException("Recipient WrappedDek missing.");

                bw.Write((byte)r.KeyId.Length);
                bw.Write(r.KeyId);

                bw.Write(r.Alg);

                if (r.WrappedDek.Length > ushort.MaxValue)
                    throw new InvalidDataException("WrappedDek too large.");

                bw.Write((ushort)r.WrappedDek.Length);
                bw.Write(r.WrappedDek);
            }

            bw.Write(HeaderSignatureAlg);

            if (HeaderSignature.Length > ushort.MaxValue)
                throw new InvalidDataException("Header signature too large.");

            bw.Write((ushort)HeaderSignature.Length);
            bw.Write(HeaderSignature);

            bw.Write(_blocks.Count);
            foreach (var b in _blocks)
                WriteBlock(bw, b);
        }

        private static void WriteBlock(BinaryWriter bw, Block b)
        {
            bw.Write(b.Index);

            byte[] titleBytes = Encoding.UTF8.GetBytes(b.Title ?? string.Empty);
            bw.Write((ushort)titleBytes.Length);
            bw.Write(titleBytes);

            bw.Write(b.CreatedUtc.UtcTicks);
            bw.Write(b.ModifiedUtc.UtcTicks);

            bw.Write(b.PrevHash);

            bw.Write(b.Nonce);
            bw.Write(b.Tag);

            bw.Write(b.Ciphertext.Length);
            bw.Write(b.Ciphertext);
        }

        // ============================================================
        // Recipient management (owner-signed)
        // ============================================================

        // Compatibility overloads: keep compilation for old call sites, but enforce owner signing.
        [Obsolete("Owner private key is required to modify recipients (header is signed). Use AddRecipient(ownerPrivateKey, recipientPublicKey).")]
        public void AddRecipient(RSA recipientPublicKey)
            => throw new InvalidOperationException("Owner private key is required to add recipients (signed header).");

        [Obsolete("Owner private key is required to modify recipients (header is signed). Use RemoveRecipientByKeyIdHex(ownerPrivateKey, keyIdHex).")]
        public bool RemoveRecipientByKeyIdHex(string keyIdHex)
            => throw new InvalidOperationException("Owner private key is required to remove recipients (signed header).");

        public void AddRecipient(RSA ownerPrivateKey, RSA recipientPublicKey)
        {
            EnsureKey();
            EnsureOwnerPrivateKeyMatches(ownerPrivateKey);

            byte[] keyId = RsaKeyFiles.ComputeKeyIdFromPublicKey(recipientPublicKey);

            if (_recipients.Any(r => r.KeyId.SequenceEqual(keyId)))
                throw new InvalidOperationException("Recipient already exists.");

            byte[] wrappedDek = recipientPublicKey.Encrypt(_key, RSAEncryptionPadding.OaepSHA256);

            _recipients.Add(new RecipientEntry
            {
                KeyId = keyId,
                Alg = RECIPIENT_ALG_RSA_OAEP_SHA256,
                WrappedDek = wrappedDek
            });

            ResignHeader(ownerPrivateKey);
        }

        public bool RemoveRecipientByKeyIdHex(RSA ownerPrivateKey, string keyIdHex)
        {
            EnsureKey();
            EnsureOwnerPrivateKeyMatches(ownerPrivateKey);

            if (string.IsNullOrWhiteSpace(keyIdHex))
                return false;

            byte[] keyId;
            try { keyId = Convert.FromHexString(keyIdHex); }
            catch { return false; }

            int removed = _recipients.RemoveAll(r => r.KeyId.SequenceEqual(keyId));
            if (removed > 0)
                ResignHeader(ownerPrivateKey);

            return removed > 0;
        }

        // ============================================================
        // Header signing / verification
        // ============================================================
        private void EnsureHeaderSigned()
        {
            if (HeaderSignature == null || HeaderSignature.Length == 0)
                throw new InvalidOperationException("Header is not signed.");
            if (HeaderSignatureAlg != HEADER_SIGALG_RSA_PSS_SHA256)
                throw new InvalidOperationException("Unsupported header signature algorithm.");
        }

        private void EnsureOwnerPrivateKeyMatches(RSA ownerPrivateKey)
        {
            if (ownerPrivateKey == null)
                throw new ArgumentNullException(nameof(ownerPrivateKey));

            byte[] spki = ownerPrivateKey.ExportSubjectPublicKeyInfo();
            if (!spki.SequenceEqual(OwnerPublicKeySpki))
                throw new CryptographicException("The provided owner private key does not match the container owner public key.");
        }

        private byte[] BuildHeaderSigningData()
        {
            // Canonical binary encoding of the header fields to be signed:
            // version(1) + encNameLen(1)+encName + ownerPubLen(2)+ownerPub +
            // recipientCount(4) + each recipient:
            //   keyIdLen(1)+keyId + alg(1) + wrappedLen(2) + wrapped
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

            bw.Write(Version);

            byte[] encBytes = Encoding.UTF8.GetBytes(EncodingWebName);
            bw.Write((byte)encBytes.Length);
            bw.Write(encBytes);

            bw.Write((ushort)OwnerPublicKeySpki.Length);
            bw.Write(OwnerPublicKeySpki);

            bw.Write(_recipients.Count);
            foreach (var r in _recipients)
            {
                bw.Write((byte)r.KeyId.Length);
                bw.Write(r.KeyId);

                bw.Write(r.Alg);

                bw.Write((ushort)r.WrappedDek.Length);
                bw.Write(r.WrappedDek);
            }

            bw.Flush();
            return ms.ToArray();
        }

        private void ResignHeader(RSA ownerPrivateKey)
        {
            EnsureOwnerPrivateKeyMatches(ownerPrivateKey);

            byte[] data = BuildHeaderSigningData();
            HeaderSignatureAlg = HEADER_SIGALG_RSA_PSS_SHA256;
            HeaderSignature = ownerPrivateKey.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        }

        private void VerifyHeaderSignatureOrThrow()
        {
            if (OwnerPublicKeySpki == null || OwnerPublicKeySpki.Length == 0)
                throw new InvalidDataException("Owner public key missing.");

            if (HeaderSignature == null || HeaderSignature.Length == 0)
                throw new InvalidDataException("Header signature missing.");

            if (HeaderSignatureAlg != HEADER_SIGALG_RSA_PSS_SHA256)
                throw new InvalidDataException("Unsupported header signature algorithm.");

            using var ownerPub = RSA.Create();
            ownerPub.ImportSubjectPublicKeyInfo(OwnerPublicKeySpki, out _);

            byte[] data = BuildHeaderSigningData();

            bool ok = ownerPub.VerifyData(data, HeaderSignature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
            if (!ok)
                throw new InvalidDataException("Header signature invalid. The file may have been tampered with.");
        }

        // ============================================================
        // Public API (blocks)
        // ============================================================
        public long GetStoredSizeBytes()
        {
            long sum = 0;
            foreach (var b in _blocks)
                sum += b.StoredSizeBytes;
            return sum;
        }

        public string GenerateUniqueTitle()
        {
            var existing = new HashSet<string>(_blocks.Select(x => x.Title), StringComparer.OrdinalIgnoreCase);
            while (true)
            {
                string s = $"Block-{Guid.NewGuid():N}";
                if (existing.Add(s))
                    return s;
            }
        }

        public Block AddRtfDocument(string title, string rtf)
        {
            EnsureKey();

            title ??= GenerateUniqueTitle();

            var b = new Block
            {
                Index = _blocks.Count,
                Title = title,
                CreatedUtc = DateTimeOffset.UtcNow,
                ModifiedUtc = DateTimeOffset.UtcNow,
                PrevHash = _blocks.Count == 0 ? new byte[Crypto.Sha256Size] : ComputeBlockHash(_blocks[^1])
            };

            byte[] plaintext = Encoding.GetEncoding(EncodingWebName).GetBytes(rtf ?? string.Empty);
            EncryptInto(b, plaintext);

            _blocks.Add(b);
            return b;
        }

        public string GetRtfDocument(int index)
        {
            EnsureKey();
            var b = GetBlock(index);

            byte[] ad = BlockAuth.BuildAssociatedData(Version, b);
            byte[] pt = Crypto.DecryptAesGcm(_key, b.Nonce, b.Ciphertext, b.Tag, ad);
            return Encoding.GetEncoding(EncodingWebName).GetString(pt);
        }

        public void UpdateRtfDocument(int index, string newRtf)
        {
            EnsureKey();
            var b = GetBlock(index);

            b.ModifiedUtc = DateTimeOffset.UtcNow;

            byte[] pt = Encoding.GetEncoding(EncodingWebName).GetBytes(newRtf ?? string.Empty);
            EncryptInto(b, pt);

            ReencryptFrom(index + 1);
        }

        public void RemoveBlock(int index)
        {
            EnsureKey();

            if ((uint)index >= (uint)_blocks.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            _blocks.RemoveAt(index);

            if (_blocks.Count == 0)
                return;

            for (int i = index; i < _blocks.Count; i++)
                _blocks[i].Index = i;

            _blocks[0].PrevHash = new byte[Crypto.Sha256Size];

            for (int i = 1; i < _blocks.Count; i++)
                _blocks[i].PrevHash = ComputeBlockHash(_blocks[i - 1]);

            ReencryptFrom(index);
        }

        public void RenameBlock(int index, string newTitle)
        {
            EnsureKey();

            if (string.IsNullOrWhiteSpace(newTitle))
                throw new ArgumentException("Title must not be empty.");

            if (_blocks.Any(x => x.Index != index && string.Equals(x.Title, newTitle, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("A block with the same title already exists.");

            var b = GetBlock(index);

            byte[] oldAd = BlockAuth.BuildAssociatedData(Version, b);
            byte[] pt = Crypto.DecryptAesGcm(_key, b.Nonce, b.Ciphertext, b.Tag, oldAd);

            b.Title = newTitle;
            b.ModifiedUtc = DateTimeOffset.UtcNow;

            EncryptInto(b, pt);

            ReencryptFrom(index + 1);
        }

        public void TransferOwnership(RSA currentOwnerPrivateKey, RSA newOwnerPrivateKey, bool ensureNewOwnerIsRecipient = true)
        {
            EnsureKey();
            EnsureOwnerPrivateKeyMatches(currentOwnerPrivateKey);

            if (newOwnerPrivateKey == null)
                throw new ArgumentNullException(nameof(newOwnerPrivateKey));

            // Ensure the new owner can open the container
            if (ensureNewOwnerIsRecipient)
            {
                using var newOwnerPub = RSA.Create();
                newOwnerPub.ImportSubjectPublicKeyInfo(newOwnerPrivateKey.ExportSubjectPublicKeyInfo(), out _);

                byte[] newKeyId = RsaKeyFiles.ComputeKeyIdFromPublicKey(newOwnerPub);
                if (!_recipients.Any(r => r.KeyId.SequenceEqual(newKeyId)))
                {
                    byte[] wrappedDek = newOwnerPub.Encrypt(_key, RSAEncryptionPadding.OaepSHA256);
                    _recipients.Add(new RecipientEntry
                    {
                        KeyId = newKeyId,
                        Alg = RECIPIENT_ALG_RSA_OAEP_SHA256,
                        WrappedDek = wrappedDek
                    });
                }
            }

            // Update owner public key and re-sign header with new owner's private key
            OwnerPublicKeySpki = newOwnerPrivateKey.ExportSubjectPublicKeyInfo();
            ResignHeader(newOwnerPrivateKey);
        }


        public byte[] ComputeBlockHash(Block b)
        {
            byte[] titleBytes = Encoding.UTF8.GetBytes(b.Title ?? string.Empty);

            Span<byte> i4 = stackalloc byte[4];
            Span<byte> i8 = stackalloc byte[8];
            Span<byte> u2 = stackalloc byte[2];

            using var sha = SHA256.Create();

            sha.TransformBlock(new[] { Version }, 0, 1, null, 0);

            BinaryPrimitives.WriteInt32LittleEndian(i4, b.Index);
            sha.TransformBlock(i4.ToArray(), 0, 4, null, 0);

            BinaryPrimitives.WriteInt64LittleEndian(i8, b.CreatedUtc.UtcTicks);
            sha.TransformBlock(i8.ToArray(), 0, 8, null, 0);

            BinaryPrimitives.WriteInt64LittleEndian(i8, b.ModifiedUtc.UtcTicks);
            sha.TransformBlock(i8.ToArray(), 0, 8, null, 0);

            sha.TransformBlock(b.PrevHash, 0, b.PrevHash.Length, null, 0);

            BinaryPrimitives.WriteUInt16LittleEndian(u2, (ushort)titleBytes.Length);
            sha.TransformBlock(u2.ToArray(), 0, 2, null, 0);
            sha.TransformBlock(titleBytes, 0, titleBytes.Length, null, 0);

            sha.TransformBlock(b.Nonce, 0, b.Nonce.Length, null, 0);
            sha.TransformBlock(b.Tag, 0, b.Tag.Length, null, 0);

            BinaryPrimitives.WriteInt32LittleEndian(i4, b.Ciphertext.Length);
            sha.TransformBlock(i4.ToArray(), 0, 4, null, 0);

            sha.TransformFinalBlock(b.Ciphertext, 0, b.Ciphertext.Length);

            return sha.Hash!;
        }

        public bool Validate(out string error)
        {
            EnsureKey();
            error = string.Empty;

            for (int i = 0; i < _blocks.Count; i++)
            {
                var b = _blocks[i];

                if (b.Index != i)
                {
                    error = $"Index mismatch at block {i}.";
                    return false;
                }

                if (i == 0)
                {
                    if (!b.PrevHash.SequenceEqual(new byte[Crypto.Sha256Size]))
                    {
                        error = "PrevHash of block 0 must be 32 zero bytes.";
                        return false;
                    }
                }
                else
                {
                    var expectedPrev = ComputeBlockHash(_blocks[i - 1]);
                    if (!b.PrevHash.SequenceEqual(expectedPrev))
                    {
                        error = $"PrevHash mismatch at block {i}.";
                        return false;
                    }
                }

                try
                {
                    byte[] ad = BlockAuth.BuildAssociatedData(Version, b);
                    _ = Crypto.DecryptAesGcm(_key, b.Nonce, b.Ciphertext, b.Tag, ad);
                }
                catch
                {
                    error = $"Authenticity check failed at block {i} (Tag mismatch).";
                    return false;
                }
            }

            return true;
        }

        // ============================================================
        // Internal chain + crypto
        // ============================================================
        private void ReencryptFrom(int startIndex)
        {
            if (startIndex <= 0) startIndex = 1;

            for (int i = startIndex; i < _blocks.Count; i++)
            {
                var prev = _blocks[i - 1];
                var cur = _blocks[i];

                byte[] oldAd = BlockAuth.BuildAssociatedData(Version, cur);
                byte[] pt = Crypto.DecryptAesGcm(_key, cur.Nonce, cur.Ciphertext, cur.Tag, oldAd);

                cur.PrevHash = ComputeBlockHash(prev);

                EncryptInto(cur, pt);
            }
        }

        private void EncryptInto(Block b, byte[] plaintext)
        {
            byte[] ad = BlockAuth.BuildAssociatedData(Version, b);
            var (nonce, ct, tag) = Crypto.EncryptAesGcm(_key, plaintext, ad);

            b.Nonce = nonce;
            b.Ciphertext = ct;
            b.Tag = tag;
        }

        private Block GetBlock(int index)
        {
            if ((uint)index >= (uint)_blocks.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _blocks[index];
        }

        private void EnsureKey()
        {
            if (_key is null || _key.Length == 0)
                throw new InvalidOperationException("Container is not open/initialized (no key in memory).");
            if (_key.Length != 32)
                throw new InvalidOperationException("Invalid DEK length in memory.");
        }

        public void CloseKeyMaterial()
        {
            if (_key.Length > 0)
            {
                Array.Clear(_key, 0, _key.Length);
                _key = Array.Empty<byte>();
            }
        }
    }

    public sealed class RecipientEntry
    {
        public byte[] KeyId { get; set; } = Array.Empty<byte>();      // 32 bytes SHA-256(SPKI)
        public byte Alg { get; set; }                                  // 1 = RSA-OAEP-SHA256
        public byte[] WrappedDek { get; set; } = Array.Empty<byte>();  // RSA-encrypted 32-byte DEK
    }
}
