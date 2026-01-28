// Container.cs (V4 current, supports loading V2/V3/V4)
// - V2: Title stored plaintext, bound into AD (legacy).
// - V3: Title is AES-GCM encrypted (separate AEAD payload), not stored plaintext.
// - V4: Per-block access control via per-block BEK (Block Encryption Key, 32 bytes) and KeySlots.
//       Each block stores KeySlots: KeyId + Rights + RSA-OAEP wrapped BEK.
//       A recipient can read a block only if they have a KeySlot for it.
// - Data encryption:
//     V2/V3: AES-GCM with container DEK (32 bytes).
//     V4: AES-GCM with per-block BEK (32 bytes).
// - Key distribution:
//     Header (V2/V3/V4): container DEK wrapped per recipient via RSA-OAEP-SHA256 (membership).
//     Blocks (V4): BEK wrapped per recipient per block via RSA-OAEP-SHA256.
// - Tamper prevention for header recipients: owner signs header via RSA-PSS-SHA256.
//   Any add/remove recipient requires owner private key to re-sign.
// - Chain: PrevHash is authenticated via AD (so edits require re-encrypt of subsequent blocks).
//   NOTE (V4): Because of chaining, editing a block requires write access to ALL subsequent blocks
//              (to re-encrypt them with updated PrevHash in AD).

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


        public void GrantReadAllBlocks(RSA ownerPrivateKey, RSA recipientPublicKey, IProgress<UiProgress> progress, CancellationToken token)
        {
            BulkSetAccessAllBlocks(ownerPrivateKey, recipientPublicKey, recipientPublicKeyKeyId: null,
                mode: BulkAccessMode.GrantReadIfMissing, rights: BlockRights.Read, progress: progress, token: token);
        }

        public void GrantWriteAllBlocks(RSA ownerPrivateKey, RSA recipientPublicKey, IProgress<UiProgress> progress, CancellationToken token)
        {
            BulkSetAccessAllBlocks(ownerPrivateKey, recipientPublicKey, recipientPublicKeyKeyId: null,
                mode: BulkAccessMode.GrantOverwrite, rights: (BlockRights.Read | BlockRights.Write), progress: progress, token: token);
        }

        public void RevokeAllBlocks(RSA ownerPrivateKey, byte[] recipientKeyId, IProgress<UiProgress> progress, CancellationToken token)
        {
            if (recipientKeyId == null || recipientKeyId.Length == 0)
                throw new ArgumentException("recipientKeyId is required.", nameof(recipientKeyId));

            BulkSetAccessAllBlocks(ownerPrivateKey, recipientPublicKey: null, recipientPublicKeyKeyId: recipientKeyId,
                mode: BulkAccessMode.Revoke, rights: BlockRights.None, progress: progress, token: token);
        }

        // ------------------ internal bulk engine (O(n)) ------------------

        private enum BulkAccessMode
        {
            GrantReadIfMissing, // only add if recipient has no slot with Read already
            GrantOverwrite,     // overwrite slot to exact rights
            Revoke              // remove slot
        }

        private void BulkSetAccessAllBlocks(
            RSA ownerPrivateKey,
            RSA? recipientPublicKey,
            byte[]? recipientPublicKeyKeyId,
            BulkAccessMode mode,
            BlockRights rights,
            IProgress<UiProgress> progress,
            CancellationToken token)
        {
            if (Version != V4)
                throw new InvalidOperationException("Bulk access operations require V4.");

            EnsureKey();
            EnsureOwnerPrivateKeyMatches(ownerPrivateKey);

            int n = _blocks.Count;
            if (n == 0)
                return;

            // Determine recipient KeyId
            byte[] recipientKeyId;
            if (recipientPublicKey != null)
            {
                byte[] spki = recipientPublicKey.ExportSubjectPublicKeyInfo();
                recipientKeyId = SHA256.HashData(spki);
            }
            else
            {
                recipientKeyId = recipientPublicKeyKeyId!;
            }

            // Ensure recipient exists in header (membership), except revoke (allow revoking even if removed later)
            if (mode != BulkAccessMode.Revoke && !_recipients.Any(r => r.KeyId.SequenceEqual(recipientKeyId)))
                throw new InvalidOperationException("Recipient is not in container recipients. AddRecipient(...) first.");

            // Phase 1: decrypt every block once (using owner slot) and capture plaintexts + BEKs
            var titles = new string[n];
            var bodies = new byte[n][];
            var beks = new byte[n][];

            progress.Report(new UiProgress
            {
                Message = "Decrypting blocks…",
                Indeterminate = false,
                Maximum = n,
                Value = 0
            });

            for (int i = 0; i < n; i++)
            {
                token.ThrowIfCancellationRequested();

                var b = _blocks[i];

                // Get BEK using owner slot
                byte[] bek = b.BlockKey ?? Array.Empty<byte>();
                if (bek.Length != 32)
                {
                    var ownerSlot = b.KeySlots.FirstOrDefault(s => s.KeyId.SequenceEqual(OwnerKeyId));
                    if (ownerSlot == null)
                        throw new CryptographicException($"Owner KeySlot missing at block {i}.");

                    if (ownerSlot.Alg != RECIPIENT_ALG_RSA_OAEP_SHA256)
                        throw new CryptographicException("Unsupported block key algorithm.");

                    bek = ownerPrivateKey.Decrypt(ownerSlot.WrappedBek, RSAEncryptionPadding.OaepSHA256);
                    if (bek == null || bek.Length != 32)
                        throw new CryptographicException("Invalid BEK length.");

                    b.BlockKey = bek; // cache for this session
                    b.MyRights = BlockRights.Read | BlockRights.Write; // owner, for this session
                }

                // Decrypt plaintext using CURRENT AD (prevhash + accessHash)
                string titlePt = DecryptTitleV4OrThrow(b);
                byte[] bodyPt = DecryptBodyV4OrThrow(b);

                titles[i] = titlePt;
                bodies[i] = bodyPt;
                beks[i] = bek;

                progress.Report(new UiProgress
                {
                    Message = $"Decrypting blocks… {i + 1}/{n}",
                    Indeterminate = false,
                    Maximum = n,
                    Value = i + 1
                });
            }

            // Phase 2: update slots (no re-encrypt yet)
            progress.Report(new UiProgress
            {
                Message = "Updating access lists…",
                Indeterminate = false,
                Maximum = n,
                Value = 0
            });

            for (int i = 0; i < n; i++)
            {
                token.ThrowIfCancellationRequested();

                var b = _blocks[i];

                if (mode == BulkAccessMode.Revoke)
                {
                    b.KeySlots.RemoveAll(s => s.KeyId.SequenceEqual(recipientKeyId));
                }
                else
                {
                    var existing = b.KeySlots.FirstOrDefault(s => s.KeyId.SequenceEqual(recipientKeyId));

                    if (mode == BulkAccessMode.GrantReadIfMissing)
                    {
                        if (existing != null && (existing.Rights & BlockRights.Read) != 0)
                        {
                            // already has read (or read+write) -> keep as-is
                            progress.Report(new UiProgress { Message = $"Updating access lists… {i + 1}/{n}", Maximum = n, Value = i + 1, Indeterminate = false });
                            continue;
                        }
                    }

                    // overwrite slot
                    if (existing != null)
                        b.KeySlots.Remove(existing);

                    byte[] wrappedBek = recipientPublicKey!.Encrypt(beks[i], RSAEncryptionPadding.OaepSHA256);
                    b.KeySlots.Add(new BlockKeySlot
                    {
                        KeyId = recipientKeyId,
                        Rights = rights,
                        Alg = RECIPIENT_ALG_RSA_OAEP_SHA256,
                        WrappedBek = wrappedBek
                    });
                }

                progress.Report(new UiProgress
                {
                    Message = $"Updating access lists… {i + 1}/{n}",
                    Indeterminate = false,
                    Maximum = n,
                    Value = i + 1
                });
            }

            // Phase 3: rebuild chain once (encrypt each block once)
            progress.Report(new UiProgress
            {
                Message = "Rebuilding chain…",
                Indeterminate = false,
                Maximum = n,
                Value = 0
            });

            // block 0 prevhash
            _blocks[0].PrevHash = ZeroHash.ToArray();

            for (int i = 0; i < n; i++)
            {
                token.ThrowIfCancellationRequested();

                var b = _blocks[i];
                b.Index = i;

                if (i == 0)
                    b.PrevHash = ZeroHash.ToArray();
                else
                    b.PrevHash = ComputeBlockHash(_blocks[i - 1]); // previous already re-encrypted

                // Keep plaintext title in memory for UI
                b.Title = titles[i];

                EncryptTitleIntoV4(b, beks[i], titles[i]);
                EncryptBodyIntoV4(b, beks[i], bodies[i]);

                progress.Report(new UiProgress
                {
                    Message = $"Rebuilding chain… {i + 1}/{n}",
                    Indeterminate = false,
                    Maximum = n,
                    Value = i + 1
                });
            }

            // cleanup references (helps GC)
            for (int i = 0; i < n; i++)
            {
                bodies[i] = Array.Empty<byte>();
                titles[i] = string.Empty;
                beks[i] = Array.Empty<byte>();
            }
        }

        // Current on-disk format
        public const byte CurrentVersion = 4;

        private const byte RECIPIENT_ALG_RSA_OAEP_SHA256 = 1;
        private const byte HEADER_SIGALG_RSA_PSS_SHA256 = 1;

        // Block AEAD purpose bytes (V3/V4)
        private const byte AD_PURPOSE_TITLE = 1;
        private const byte AD_PURPOSE_BODY = 2;

        // Versions
        private const byte V2 = 2;
        private const byte V3 = 3;
        private const byte V4 = 4;

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

        // Container DEK (membership key) held in memory while container is open (AES-256)
        // - Used to decrypt V2/V3 blocks.
        // - In V4, this key is only used to prove membership / open the container.
        private byte[] _key = Array.Empty<byte>();

        // Active user's KeyId (SHA256(SPKI(pub-from-private)))
        private byte[] _activeKeyId = Array.Empty<byte>();

        // Cached encoding for RTF bytes
        private Encoding? _encCache;

        private static readonly byte[] ZeroHash = new byte[Crypto.Sha256Size];

        private HSTRYContainer() { }

        private Encoding GetContainerEncoding()
            => _encCache ??= Encoding.GetEncoding(EncodingWebName);

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
        /// Create a new container (V4 current).
        /// - ownerPrivateKey signs the header.
        /// - recipients are the public keys that can open the container (should include owner).
        /// - By default, new blocks grant Read to all recipients and Read|Write to owner.
        /// </summary>
        // =======================
        // FIXED (complete): CreateNewForRecipients (sets _activeKeyId for new container session)
        // Variant 1 semantics: blocks default to Owner RW, others None (handled in AddRtfDocument).
        // =======================
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
            c._encCache = null;

            c.OwnerPublicKeySpki = ownerPrivateKey.ExportSubjectPublicKeyInfo();
            c.HeaderSignatureAlg = HEADER_SIGALG_RSA_PSS_SHA256;

            // IMPORTANT: set active identity for this in-memory session (creator = owner)
            c._activeKeyId = SHA256.HashData(c.OwnerPublicKeySpki);

            // Generate container DEK (membership key, AES-256)
            c._key = new byte[32];
            RandomNumberGenerator.Fill(c._key);

            // Wrap container DEK for each recipient, and store their SPKI in V4
            foreach (var rsaPub in pubs)
            {
                byte[] spki = rsaPub.ExportSubjectPublicKeyInfo();
                byte[] keyId = SHA256.HashData(spki);
                byte[] wrappedDek = rsaPub.Encrypt(c._key, RSAEncryptionPadding.OaepSHA256);

                c._recipients.Add(new RecipientEntry
                {
                    KeyId = keyId,
                    PublicKeySpki = spki,
                    Alg = RECIPIENT_ALG_RSA_OAEP_SHA256,
                    WrappedDek = wrappedDek
                });
            }

            // Ensure unique KeyIds
            if (c._recipients.Select(r => Convert.ToHexString(r.KeyId))
                .Distinct(StringComparer.OrdinalIgnoreCase).Count() != c._recipients.Count)
                throw new InvalidOperationException("Duplicate recipient keys detected (KeyId collision).");

            // Sign header
            c.ResignHeader(ownerPrivateKey);

            return c;
        }


        public static HSTRYContainer LoadWithPrivateKeyFile(string containerPath, string privateKeyPath)
        {
            // Large buffering improves throughput for big containers
            using var fs = new FileStream(containerPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1 << 20);
            using var bs = new BufferedStream(fs, 1 << 20);
            using var rsaPriv = RsaKeyFiles.LoadPrivateKeyPkcs8(privateKeyPath);
            return LoadWithPrivateKey(bs, rsaPriv);
        }

        public static HSTRYContainer LoadWithPrivateKey(Stream stream, RSA privateKey)
        {
            if (privateKey == null) throw new ArgumentNullException(nameof(privateKey));

            using var br = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

            byte[] magic = br.ReadBytes(Global.FileMagic.Length);
            if (!magic.SequenceEqual(Global.FileMagic))
                throw new InvalidDataException("Invalid file format (magic mismatch).");

            byte version = br.ReadByte();
            if (version != V2 && version != V3 && version != V4)
                throw new InvalidDataException($"Unsupported container version: {version}.");

            var c = new HSTRYContainer { Version = version };

            // ----- Header (unsigned parts first) -----
            int encNameLen = br.ReadByte();
            c.EncodingWebName = Encoding.UTF8.GetString(br.ReadBytes(encNameLen));
            c._encCache = null;

            ushort ownerPubLen = br.ReadUInt16();
            c.OwnerPublicKeySpki = br.ReadBytes(ownerPubLen);

            int recipientCount = br.ReadInt32();
            if (recipientCount <= 0)
                throw new InvalidDataException("Recipient list is empty.");

            for (int i = 0; i < recipientCount; i++)
            {
                int keyIdLen = br.ReadByte();
                byte[] keyId = br.ReadBytes(keyIdLen);

                byte[] spki = Array.Empty<byte>();
                if (version == V4)
                {
                    ushort spkiLen = br.ReadUInt16();
                    if (spkiLen == 0) throw new InvalidDataException("Recipient SPKI missing (V4).");
                    spki = br.ReadBytes(spkiLen);

                    // Sanity check KeyId matches SPKI
                    byte[] check = SHA256.HashData(spki);
                    if (!check.SequenceEqual(keyId))
                        throw new InvalidDataException("Recipient KeyId does not match SPKI (V4).");
                }

                byte alg = br.ReadByte();

                ushort wrappedLen = br.ReadUInt16();
                byte[] wrappedDek = br.ReadBytes(wrappedLen);

                c._recipients.Add(new RecipientEntry
                {
                    KeyId = keyId,
                    PublicKeySpki = spki,
                    Alg = alg,
                    WrappedDek = wrappedDek
                });
            }

            c.HeaderSignatureAlg = br.ReadByte();
            ushort sigLen = br.ReadUInt16();
            c.HeaderSignature = br.ReadBytes(sigLen);

            // Verify signature FIRST (prevents recipient/header tampering)
            c.VerifyHeaderSignatureOrThrow();

            // Active KeyId derived from this private key
            c._activeKeyId = RsaKeyFiles.ComputeKeyIdFromPublicKey(privateKey);

            // Find matching recipient by KeyId
            var entry = c._recipients.FirstOrDefault(r => r.KeyId.SequenceEqual(c._activeKeyId));
            if (entry == null)
                throw new CryptographicException("No matching recipient entry for this private key.");

            if (entry.Alg != RECIPIENT_ALG_RSA_OAEP_SHA256)
                throw new CryptographicException("Unsupported recipient key algorithm.");

            // Unwrap container DEK (membership key)
            byte[] dek;
            try
            {
                dek = privateKey.Decrypt(entry.WrappedDek, RSAEncryptionPadding.OaepSHA256);
            }
            catch
            {
                throw new CryptographicException("Failed to decrypt container key with the provided private key.");
            }

            if (dek == null || dek.Length != 32)
                throw new CryptographicException("Invalid container key length.");

            c._key = dek;

            // ----- Blocks -----
            int blockCount = br.ReadInt32();
            for (int i = 0; i < blockCount; i++)
            {
                var b = new Block
                {
                    Index = br.ReadInt32()
                };

                if (version == V2)
                {
                    // V2: plaintext title
                    ushort titleLen = br.ReadUInt16();
                    b.Title = Encoding.UTF8.GetString(br.ReadBytes(titleLen));

                    b.CreatedUtc = new DateTimeOffset(br.ReadInt64(), TimeSpan.Zero);
                    b.ModifiedUtc = new DateTimeOffset(br.ReadInt64(), TimeSpan.Zero);

                    b.PrevHash = br.ReadBytes(Crypto.Sha256Size);

                    b.Nonce = br.ReadBytes(Crypto.AesGcmNonceSize);
                    b.Tag = br.ReadBytes(Crypto.AesGcmTagSize);

                    int ctLen = br.ReadInt32();
                    b.Ciphertext = br.ReadBytes(ctLen);
                }
                else if (version == V3)
                {
                    // V3: encrypted title + encrypted body with container DEK
                    b.CreatedUtc = new DateTimeOffset(br.ReadInt64(), TimeSpan.Zero);
                    b.ModifiedUtc = new DateTimeOffset(br.ReadInt64(), TimeSpan.Zero);

                    b.PrevHash = br.ReadBytes(Crypto.Sha256Size);

                    // Title payload
                    b.TitleNonce = br.ReadBytes(Crypto.AesGcmNonceSize);
                    b.TitleTag = br.ReadBytes(Crypto.AesGcmTagSize);

                    int titleCtLen = br.ReadInt32();
                    if (titleCtLen < 0) throw new InvalidDataException("Invalid title ciphertext length.");
                    b.TitleCiphertext = br.ReadBytes(titleCtLen);

                    // Body payload
                    b.Nonce = br.ReadBytes(Crypto.AesGcmNonceSize);
                    b.Tag = br.ReadBytes(Crypto.AesGcmTagSize);

                    int ctLen = br.ReadInt32();
                    if (ctLen < 0) throw new InvalidDataException("Invalid ciphertext length.");
                    b.Ciphertext = br.ReadBytes(ctLen);

                    // Title plaintext hydrated during Validate()
                    b.Title = string.Empty;
                }
                else // V4
                {
                    b.CreatedUtc = new DateTimeOffset(br.ReadInt64(), TimeSpan.Zero);
                    b.ModifiedUtc = new DateTimeOffset(br.ReadInt64(), TimeSpan.Zero);

                    b.PrevHash = br.ReadBytes(Crypto.Sha256Size);

                    int slotCount = br.ReadInt32();
                    if (slotCount < 0) throw new InvalidDataException("Invalid KeySlot count (V4).");

                    for (int s = 0; s < slotCount; s++)
                    {
                        int kidLen = br.ReadByte();
                        byte[] kid = br.ReadBytes(kidLen);

                        var rights = (BlockRights)br.ReadByte();
                        byte alg = br.ReadByte();

                        ushort wlen = br.ReadUInt16();
                        byte[] wrappedBek = br.ReadBytes(wlen);

                        b.KeySlots.Add(new BlockKeySlot
                        {
                            KeyId = kid,
                            Rights = rights,
                            Alg = alg,
                            WrappedBek = wrappedBek
                        });
                    }

                    // Try unwrap BEK for active user (optional)
                    var mySlot = b.KeySlots.FirstOrDefault(x => x.KeyId.SequenceEqual(c._activeKeyId));
                    if (mySlot != null)
                    {
                        if (mySlot.Alg != RECIPIENT_ALG_RSA_OAEP_SHA256)
                            throw new CryptographicException("Unsupported block key algorithm.");

                        try
                        {
                            byte[] bek = privateKey.Decrypt(mySlot.WrappedBek, RSAEncryptionPadding.OaepSHA256);
                            if (bek == null || bek.Length != 32)
                                throw new CryptographicException("Invalid BEK length.");
                            b.BlockKey = bek;
                            b.MyRights = mySlot.Rights;
                        }
                        catch
                        {
                            throw new CryptographicException("Failed to decrypt BEK for a block.");
                        }
                    }

                    // Title payload
                    b.TitleNonce = br.ReadBytes(Crypto.AesGcmNonceSize);
                    b.TitleTag = br.ReadBytes(Crypto.AesGcmTagSize);

                    int titleCtLen = br.ReadInt32();
                    if (titleCtLen < 0) throw new InvalidDataException("Invalid title ciphertext length.");
                    b.TitleCiphertext = br.ReadBytes(titleCtLen);

                    // Body payload
                    b.Nonce = br.ReadBytes(Crypto.AesGcmNonceSize);
                    b.Tag = br.ReadBytes(Crypto.AesGcmTagSize);

                    int ctLen = br.ReadInt32();
                    if (ctLen < 0) throw new InvalidDataException("Invalid ciphertext length.");
                    b.Ciphertext = br.ReadBytes(ctLen);

                    b.Title = string.Empty; // hydrated for accessible blocks
                }

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
            // Large buffering improves throughput for big containers
            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 1 << 20);
            using var bs = new BufferedStream(fs, 1 << 20);
            Save(bs);
        }

        public void Save(Stream stream)
        {
            EnsureKey();

            if (Version != V4)
                throw new InvalidOperationException("This container is not V4. Call UpgradeToV4(...) first and then Save().");

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
                if (r.PublicKeySpki == null || r.PublicKeySpki.Length == 0)
                    throw new InvalidDataException("Recipient SPKI missing (V4).");
                if (r.WrappedDek == null || r.WrappedDek.Length == 0)
                    throw new InvalidDataException("Recipient WrappedDek missing.");

                bw.Write((byte)r.KeyId.Length);
                bw.Write(r.KeyId);

                if (r.PublicKeySpki.Length > ushort.MaxValue)
                    throw new InvalidDataException("Recipient SPKI too large.");
                bw.Write((ushort)r.PublicKeySpki.Length);
                bw.Write(r.PublicKeySpki);

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
                WriteBlockV4(bw, b);
        }

        private static void WriteBlockV4(BinaryWriter bw, Block b)
        {
            bw.Write(b.Index);

            bw.Write(b.CreatedUtc.UtcTicks);
            bw.Write(b.ModifiedUtc.UtcTicks);

            bw.Write(b.PrevHash);

            // KeySlots
            bw.Write(b.KeySlots.Count);
            foreach (var s in b.KeySlots)
            {
                if (s.KeyId == null || s.KeyId.Length == 0)
                    throw new InvalidDataException("Block KeySlot KeyId missing.");
                if (s.WrappedBek == null || s.WrappedBek.Length == 0)
                    throw new InvalidDataException("Block KeySlot WrappedBek missing.");
                if (s.WrappedBek.Length > ushort.MaxValue)
                    throw new InvalidDataException("Block KeySlot WrappedBek too large.");

                bw.Write((byte)s.KeyId.Length);
                bw.Write(s.KeyId);

                bw.Write((byte)s.Rights);
                bw.Write(s.Alg);

                bw.Write((ushort)s.WrappedBek.Length);
                bw.Write(s.WrappedBek);
            }

            // Title payload
            if (b.TitleCiphertext == null || b.TitleCiphertext.Length == 0)
                throw new InvalidDataException("Encrypted title is missing (V4).");
            if (b.TitleNonce == null || b.TitleNonce.Length != Crypto.AesGcmNonceSize)
                throw new InvalidDataException("Title nonce missing/invalid (V4).");
            if (b.TitleTag == null || b.TitleTag.Length != Crypto.AesGcmTagSize)
                throw new InvalidDataException("Title tag missing/invalid (V4).");

            bw.Write(b.TitleNonce);
            bw.Write(b.TitleTag);
            bw.Write(b.TitleCiphertext.Length);
            bw.Write(b.TitleCiphertext);

            // Body payload
            bw.Write(b.Nonce);
            bw.Write(b.Tag);
            bw.Write(b.Ciphertext.Length);
            bw.Write(b.Ciphertext);
        }

        // ============================================================
        // Recipient management (owner-signed)
        // ============================================================

        public void AddRecipient(RSA ownerPrivateKey, RSA recipientPublicKey)
        {
            EnsureKey();
            EnsureOwnerPrivateKeyMatches(ownerPrivateKey);

            byte[] spki = recipientPublicKey.ExportSubjectPublicKeyInfo();
            byte[] keyId = SHA256.HashData(spki);

            if (_recipients.Any(r => r.KeyId.SequenceEqual(keyId)))
                throw new InvalidOperationException("Recipient already exists.");

            byte[] wrappedDek = recipientPublicKey.Encrypt(_key, RSAEncryptionPadding.OaepSHA256);

            _recipients.Add(new RecipientEntry
            {
                KeyId = keyId,
                PublicKeySpki = spki,
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
        // Block access control (V4)
        // ============================================================

        public void GrantBlockAccess(RSA ownerPrivateKey, int blockIndex, RSA recipientPublicKey, BlockRights rights, bool replaceExisting = true)
        {
            if (Version != V4)
                throw new InvalidOperationException("Block access control is only available in V4.");

            EnsureKey();
            EnsureOwnerPrivateKeyMatches(ownerPrivateKey);

            if ((uint)blockIndex >= (uint)_blocks.Count)
                throw new ArgumentOutOfRangeException(nameof(blockIndex));

            if (recipientPublicKey == null)
                throw new ArgumentNullException(nameof(recipientPublicKey));

            byte[] spki = recipientPublicKey.ExportSubjectPublicKeyInfo();
            byte[] keyId = SHA256.HashData(spki);

            if (!_recipients.Any(r => r.KeyId.SequenceEqual(keyId)))
                throw new InvalidOperationException("Recipient is not in container recipients. AddRecipient(...) first.");

            var b = _blocks[blockIndex];

            EnsureWritableFrom(blockIndex); // chain requirement

            if (b.BlockKey == null || b.BlockKey.Length != 32)
                throw new UnauthorizedAccessException("No access to block key (BEK).");

            // Decrypt current plaintext BEFORE changing slots (slots are part of AD in V4).
            string titlePt = DecryptTitleV4OrThrow(b);
            byte[] bodyPt = DecryptBodyV4OrThrow(b);

            // Update slots
            if (replaceExisting)
                b.KeySlots.RemoveAll(s => s.KeyId.SequenceEqual(keyId));

            byte[] wrappedBek = recipientPublicKey.Encrypt(b.BlockKey, RSAEncryptionPadding.OaepSHA256);
            b.KeySlots.Add(new BlockKeySlot
            {
                KeyId = keyId,
                Rights = rights,
                Alg = RECIPIENT_ALG_RSA_OAEP_SHA256,
                WrappedBek = wrappedBek
            });

            // Re-encrypt this block (AD changed), then fix chain forward
            EncryptTitleIntoV4(b, b.BlockKey, titlePt);
            EncryptBodyIntoV4(b, b.BlockKey, bodyPt);

            ReencryptFromV4(blockIndex + 1);
        }

        public void RevokeBlockAccess(RSA ownerPrivateKey, int blockIndex, string keyIdHex)
        {
            if (Version != V4)
                throw new InvalidOperationException("Block access control is only available in V4.");

            EnsureKey();
            EnsureOwnerPrivateKeyMatches(ownerPrivateKey);

            if ((uint)blockIndex >= (uint)_blocks.Count)
                throw new ArgumentOutOfRangeException(nameof(blockIndex));

            if (string.IsNullOrWhiteSpace(keyIdHex))
                throw new ArgumentException("KeyId is required.");

            byte[] keyId = Convert.FromHexString(keyIdHex);

            var b = _blocks[blockIndex];

            EnsureWritableFrom(blockIndex); // chain requirement

            if (b.BlockKey == null || b.BlockKey.Length != 32)
                throw new UnauthorizedAccessException("No access to block key (BEK).");

            // Decrypt current plaintext BEFORE changing slots
            string titlePt = DecryptTitleV4OrThrow(b);
            byte[] bodyPt = DecryptBodyV4OrThrow(b);

            int removed = b.KeySlots.RemoveAll(s => s.KeyId.SequenceEqual(keyId));
            if (removed == 0)
                return;

            // Re-encrypt this block (AD changed), then fix chain forward
            EncryptTitleIntoV4(b, b.BlockKey, titlePt);
            EncryptBodyIntoV4(b, b.BlockKey, bodyPt);

            ReencryptFromV4(blockIndex + 1);
        }

        // ============================================================
        // Upgrade to V4
        // ============================================================

        /// <summary>
        /// Upgrades an opened V2/V3 container to V4. You must provide all existing recipients' PUBLIC KEYS,
        /// because V2/V3 headers do not store recipients' SPKIs.
        /// </summary>
        public void UpgradeToV4(RSA ownerPrivateKey, IEnumerable<RSA> recipientPublicKeys, BlockRights nonOwnerDefaultRights = BlockRights.Read)
        {
            EnsureKey();
            EnsureOwnerPrivateKeyMatches(ownerPrivateKey);

            if (Version == V4)
                return;

            if (Version != V2 && Version != V3)
                throw new InvalidOperationException("Only V2/V3 containers can be upgraded.");

            if (recipientPublicKeys == null)
                throw new ArgumentNullException(nameof(recipientPublicKeys));

            var pubList = recipientPublicKeys.ToList();
            if (pubList.Count == 0)
                throw new ArgumentException("At least one recipient public key is required.");

            // Map KeyId -> public key SPKI
            var map = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            foreach (var rsaPub in pubList)
            {
                byte[] spki = rsaPub.ExportSubjectPublicKeyInfo();
                string kid = Convert.ToHexString(SHA256.HashData(spki));
                map[kid] = spki;
            }

            // Ensure all existing recipients are provided
            foreach (var r in _recipients)
            {
                string kid = Convert.ToHexString(r.KeyId);
                if (!map.ContainsKey(kid))
                    throw new InvalidOperationException($"Missing public key SPKI for existing recipient {kid}.");
            }

            // Decrypt plaintexts from old format
            var titles = new List<string>(_blocks.Count);
            var bodies = new List<byte[]>(_blocks.Count);

            for (int i = 0; i < _blocks.Count; i++)
            {
                var b = _blocks[i];

                if (Version == V2)
                {
                    titles.Add(b.Title);
                    byte[] ad = BlockAuth.BuildAssociatedData(V2, b, 0);
                    byte[] pt = Crypto.DecryptAesGcm(_key, b.Nonce, b.Ciphertext, b.Tag, ad);
                    bodies.Add(pt);
                }
                else // V3
                {
                    // V3 title
                    byte[] adTitle = BlockAuth.BuildAssociatedData(V3, b, AD_PURPOSE_TITLE);
                    byte[] titlePt = Crypto.DecryptAesGcm(_key, b.TitleNonce, b.TitleCiphertext, b.TitleTag, adTitle);
                    titles.Add(Encoding.UTF8.GetString(titlePt));

                    // V3 body
                    byte[] adBody = BlockAuth.BuildAssociatedData(V3, b, AD_PURPOSE_BODY);
                    byte[] bodyPt = Crypto.DecryptAesGcm(_key, b.Nonce, b.Ciphertext, b.Tag, adBody);
                    bodies.Add(bodyPt);
                }
            }

            // Build V4 recipient list with SPKI and fresh wrapped DEK
            var newRecipients = new List<RecipientEntry>(_recipients.Count);

            foreach (var old in _recipients)
            {
                string kid = Convert.ToHexString(old.KeyId);
                byte[] spki = map[kid];

                using var rsaPub = RSA.Create();
                rsaPub.ImportSubjectPublicKeyInfo(spki, out _);

                byte[] wrappedDek = rsaPub.Encrypt(_key, RSAEncryptionPadding.OaepSHA256);

                newRecipients.Add(new RecipientEntry
                {
                    KeyId = old.KeyId,
                    PublicKeySpki = spki,
                    Alg = RECIPIENT_ALG_RSA_OAEP_SHA256,
                    WrappedDek = wrappedDek
                });
            }

            _recipients.Clear();
            _recipients.AddRange(newRecipients);

            // Switch version to V4 and rebuild blocks with per-block BEKs + slots
            Version = V4;

            if (_blocks.Count > 0)
                _blocks[0].PrevHash = ZeroHash.ToArray();

            for (int i = 0; i < _blocks.Count; i++)
            {
                var b = _blocks[i];
                b.Index = i;

                if (i == 0)
                    b.PrevHash = ZeroHash.ToArray();
                else
                    b.PrevHash = ComputeBlockHash(_blocks[i - 1]);

                // New BEK for each block
                b.BlockKey = new byte[32];
                RandomNumberGenerator.Fill(b.BlockKey);

                // Slots: owner RW, others default
                b.KeySlots.Clear();
                foreach (var r in _recipients)
                {
                    using var rsaPub = RSA.Create();
                    rsaPub.ImportSubjectPublicKeyInfo(r.PublicKeySpki, out _);

                    var rights = r.KeyId.SequenceEqual(OwnerKeyId) ? (BlockRights.Read | BlockRights.Write) : nonOwnerDefaultRights;

                    byte[] wrappedBek = rsaPub.Encrypt(b.BlockKey, RSAEncryptionPadding.OaepSHA256);

                    b.KeySlots.Add(new BlockKeySlot
                    {
                        KeyId = r.KeyId,
                        Rights = rights,
                        Alg = RECIPIENT_ALG_RSA_OAEP_SHA256,
                        WrappedBek = wrappedBek
                    });
                }

                // Encrypt title + body with BEK
                b.Title = titles[i] ?? string.Empty;
                EncryptTitleIntoV4(b, b.BlockKey, b.Title);
                EncryptBodyIntoV4(b, b.BlockKey, bodies[i]);
            }

            // Re-sign header (recipient layout changed to V4)
            ResignHeader(ownerPrivateKey);

            if (!Validate(out string err))
                throw new InvalidDataException("Upgrade failed: " + err);
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

                if (Version == V4)
                {
                    bw.Write((ushort)r.PublicKeySpki.Length);
                    bw.Write(r.PublicKeySpki);
                }

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

        /// <summary>
        /// Adds a block. Default access:
        /// - Owner: Read|Write
        /// - Other recipients: Read
        /// </summary>
        // =======================
        // FIXED (complete): AddRtfDocument (Variant 1: owner RW, others None)
        // =======================
        public Block AddRtfDocument(string title, string rtf)
        {
            if (Version != V4)
                throw new InvalidOperationException("AddRtfDocument is only supported in V4.");

            EnsureKey();

            // Safety: if container was created in-memory without LoadWithPrivateKey,
            // ensure active identity is at least owner
            if (_activeKeyId == null || _activeKeyId.Length == 0)
                _activeKeyId = OwnerKeyId;

            title ??= GenerateUniqueTitle();

            var b = new Block
            {
                Index = _blocks.Count,
                Title = title,
                CreatedUtc = DateTimeOffset.UtcNow,
                ModifiedUtc = DateTimeOffset.UtcNow,
                PrevHash = _blocks.Count == 0 ? ZeroHash.ToArray() : ComputeBlockHash(_blocks[^1])
            };

            // Generate BEK
            b.BlockKey = new byte[32];
            RandomNumberGenerator.Fill(b.BlockKey);

            // Variant 1: Default slots: owner Read|Write, everyone else None
            b.KeySlots.Clear();
            foreach (var r in _recipients)
            {
                using var rsaPub = RSA.Create();
                rsaPub.ImportSubjectPublicKeyInfo(r.PublicKeySpki, out _);

                var rights = r.KeyId.SequenceEqual(OwnerKeyId)
                    ? (BlockRights.Read | BlockRights.Write)
                    : BlockRights.None;

                byte[] wrappedBek = rsaPub.Encrypt(b.BlockKey, RSAEncryptionPadding.OaepSHA256);

                b.KeySlots.Add(new BlockKeySlot
                {
                    KeyId = r.KeyId,
                    Rights = rights,
                    Alg = RECIPIENT_ALG_RSA_OAEP_SHA256,
                    WrappedBek = wrappedBek
                });

                // Cache rights for active user if this is them
                if (r.KeyId.SequenceEqual(_activeKeyId))
                    b.MyRights = rights;
            }

            // Encrypt title + body with BEK
            EncryptTitleIntoV4(b, b.BlockKey, title);

            byte[] plaintext = GetContainerEncoding().GetBytes(rtf ?? string.Empty);
            EncryptBodyIntoV4(b, b.BlockKey, plaintext);

            _blocks.Add(b);
            return b;
        }

        public string GetRtfDocument(int index)
        {
            EnsureKey();
            var b = GetBlock(index);

            if (Version == V2 || Version == V3)
            {
                byte[] ad = BlockAuth.BuildAssociatedData(Version, b, (Version == V2) ? (byte)0 : AD_PURPOSE_BODY);
                byte[] pt = Crypto.DecryptAesGcm(_key, b.Nonce, b.Ciphertext, b.Tag, ad);
                return GetContainerEncoding().GetString(pt);
            }

            // V4
            if ((b.MyRights & BlockRights.Read) == 0 || b.BlockKey == null)
                throw new UnauthorizedAccessException("No read access to this block.");

            byte[] adBody = BlockAuth.BuildAssociatedData(V4, b, AD_PURPOSE_BODY);
            byte[] bodyPt = Crypto.DecryptAesGcm(b.BlockKey, b.Nonce, b.Ciphertext, b.Tag, adBody);
            return GetContainerEncoding().GetString(bodyPt);
        }

        // =======================
        // FIXED (complete): UpdateRtfDocument (V4) – correct AD order + rollback on failure
        // =======================
        public void UpdateRtfDocument(int index, string newRtf)
        {
            EnsureKey();
            var b = GetBlock(index);

            if (Version != V4)
                throw new InvalidOperationException("UpdateRtfDocument is only supported in V4.");

            // Chain requirement: write access to this + all subsequent blocks
            EnsureWritableFrom(index);

            if (b.BlockKey == null)
                throw new UnauthorizedAccessException("No access to this block.");

            // IMPORTANT: decrypt using OLD AD first (before changing ModifiedUtc)
            DateTimeOffset oldModified = b.ModifiedUtc;

            string oldTitle;
            try
            {
                oldTitle = DecryptTitleV4OrThrow(b); // uses current (old) ModifiedUtc
                                                     // Optional sanity check (can be removed for speed):
                _ = DecryptBodyV4OrThrow(b);
            }
            catch
            {
                // do not mutate anything if decrypt fails
                throw;
            }

            try
            {
                b.ModifiedUtc = DateTimeOffset.UtcNow;

                // Re-encrypt title because ModifiedUtc is in AD
                EncryptTitleIntoV4(b, b.BlockKey, oldTitle);

                // Encrypt new body
                byte[] pt = GetContainerEncoding().GetBytes(newRtf ?? string.Empty);
                EncryptBodyIntoV4(b, b.BlockKey, pt);

                // Fix chain forward
                ReencryptFromV4(index + 1);
            }
            catch
            {
                // Roll back ModifiedUtc if anything fails (prevents corruption)
                b.ModifiedUtc = oldModified;
                throw;
            }
        }

        public void RemoveBlock(int index)
        {
            EnsureKey();

            if (Version != V4)
                throw new InvalidOperationException("RemoveBlock is only supported in V4.");

            if ((uint)index >= (uint)_blocks.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            EnsureWritableFrom(index); // chain requirement (renumber + prevhash updates)

            _blocks.RemoveAt(index);

            if (_blocks.Count == 0)
                return;

            // Renumber
            for (int i = index; i < _blocks.Count; i++)
                _blocks[i].Index = i;

            // Fix block 0 prevhash
            _blocks[0].PrevHash = ZeroHash.ToArray();

            // Re-encrypt from index (chain must be rebuilt due to index and prevhash changes)
            ReencryptFromV4(index);
        }

        public void RenameBlock(int index, string newTitle)
        {
            EnsureKey();

            if (Version != V4)
                throw new InvalidOperationException("RenameBlock is only supported in V4.");

            if (string.IsNullOrWhiteSpace(newTitle))
                throw new ArgumentException("Title must not be empty.");

            if (_blocks.Any(x => x.Index != index && string.Equals(x.Title, newTitle, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("A block with the same title already exists.");

            var b = GetBlock(index);

            EnsureWritableFrom(index);

            if (b.BlockKey == null)
                throw new UnauthorizedAccessException("No access to this block.");

            // Decrypt body first (ModifiedUtc changes => AD changes)
            byte[] oldAdBody = BlockAuth.BuildAssociatedData(V4, b, AD_PURPOSE_BODY);
            byte[] bodyPt = Crypto.DecryptAesGcm(b.BlockKey, b.Nonce, b.Ciphertext, b.Tag, oldAdBody);

            b.Title = newTitle;
            b.ModifiedUtc = DateTimeOffset.UtcNow;

            EncryptTitleIntoV4(b, b.BlockKey, b.Title);
            EncryptBodyIntoV4(b, b.BlockKey, bodyPt);

            ReencryptFromV4(index + 1);
        }

        public void TransferOwnership(RSA currentOwnerPrivateKey, RSA newOwnerPrivateKey, bool ensureNewOwnerIsRecipient = true)
        {
            EnsureKey();
            EnsureOwnerPrivateKeyMatches(currentOwnerPrivateKey);

            if (newOwnerPrivateKey == null)
                throw new ArgumentNullException(nameof(newOwnerPrivateKey));

            if (Version != V4)
                throw new InvalidOperationException("TransferOwnership is only supported in V4.");

            // Ensure the new owner can open the container
            if (ensureNewOwnerIsRecipient)
            {
                using var newOwnerPub = RSA.Create();
                newOwnerPub.ImportSubjectPublicKeyInfo(newOwnerPrivateKey.ExportSubjectPublicKeyInfo(), out _);

                byte[] newSpki = newOwnerPub.ExportSubjectPublicKeyInfo();
                byte[] newKeyId = SHA256.HashData(newSpki);

                if (!_recipients.Any(r => r.KeyId.SequenceEqual(newKeyId)))
                {
                    byte[] wrappedDek = newOwnerPub.Encrypt(_key, RSAEncryptionPadding.OaepSHA256);
                    _recipients.Add(new RecipientEntry
                    {
                        KeyId = newKeyId,
                        PublicKeySpki = newSpki,
                        Alg = RECIPIENT_ALG_RSA_OAEP_SHA256,
                        WrappedDek = wrappedDek
                    });
                }
            }

            // Update owner public key and re-sign header with new owner's private key
            OwnerPublicKeySpki = newOwnerPrivateKey.ExportSubjectPublicKeyInfo();
            ResignHeader(newOwnerPrivateKey);
        }

        // ============================================================
        // Hash / Validate
        // ============================================================

        public byte[] ComputeBlockHash(Block b)
        {
            Span<byte> b1 = stackalloc byte[1];
            Span<byte> i4 = stackalloc byte[4];
            Span<byte> i8 = stackalloc byte[8];

            using var ih = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

            b1[0] = Version;
            ih.AppendData(b1);

            BinaryPrimitives.WriteInt32LittleEndian(i4, b.Index);
            ih.AppendData(i4);

            BinaryPrimitives.WriteInt64LittleEndian(i8, b.CreatedUtc.UtcTicks);
            ih.AppendData(i8);

            BinaryPrimitives.WriteInt64LittleEndian(i8, b.ModifiedUtc.UtcTicks);
            ih.AppendData(i8);

            ih.AppendData(b.PrevHash);

            if (Version == V2)
            {
                byte[] titleBytes = Encoding.UTF8.GetBytes(b.Title ?? string.Empty);
                Span<byte> u2 = stackalloc byte[2];
                BinaryPrimitives.WriteUInt16LittleEndian(u2, (ushort)titleBytes.Length);
                ih.AppendData(u2);
                ih.AppendData(titleBytes);
            }
            else
            {
                // V3/V4 title payload
                Span<byte> tlen = stackalloc byte[4];
                BinaryPrimitives.WriteInt32LittleEndian(tlen, b.TitleCiphertext?.Length ?? 0);

                ih.AppendData(b.TitleNonce);
                ih.AppendData(b.TitleTag);
                ih.AppendData(tlen);
                if (b.TitleCiphertext != null && b.TitleCiphertext.Length > 0)
                    ih.AppendData(b.TitleCiphertext);
            }

            if (Version == V4)
            {
                // Bind access control metadata into block hash
                byte[] accessHash = BlockAuth.ComputeAccessHashV4(b);
                ih.AppendData(accessHash);
            }

            // Body payload
            Span<byte> clen = stackalloc byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(clen, b.Ciphertext?.Length ?? 0);

            ih.AppendData(b.Nonce);
            ih.AppendData(b.Tag);
            ih.AppendData(clen);

            if (b.Ciphertext != null && b.Ciphertext.Length > 0)
                ih.AppendData(b.Ciphertext);

            return ih.GetHashAndReset();
        }

        public bool Validate(out string error)
        {
            EnsureKey();
            error = string.Empty;

            if (_blocks.Count == 0)
                return true;

            // block 0 checks
            if (_blocks[0].Index != 0)
            {
                error = "Index mismatch at block 0.";
                return false;
            }

            if (!_blocks[0].PrevHash.SequenceEqual(ZeroHash))
            {
                error = "PrevHash of block 0 must be 32 zero bytes.";
                return false;
            }

            // Validate block 0 (if accessible)
            if (!ValidateAndHydrateBlock(_blocks[0], out error))
                return false;

            byte[] prevHash = ComputeBlockHash(_blocks[0]);

            for (int i = 1; i < _blocks.Count; i++)
            {
                var b = _blocks[i];

                if (b.Index != i)
                {
                    error = $"Index mismatch at block {i}.";
                    return false;
                }

                if (!b.PrevHash.SequenceEqual(prevHash))
                {
                    error = $"PrevHash mismatch at block {i}.";
                    return false;
                }

                if (!ValidateAndHydrateBlock(b, out error))
                    return false;

                prevHash = ComputeBlockHash(b);
            }

            return true;
        }

        private bool ValidateAndHydrateBlock(Block b, out string error)
        {
            error = string.Empty;

            try
            {
                if (Version == V2)
                {
                    byte[] ad = BlockAuth.BuildAssociatedData(V2, b, 0);
                    _ = Crypto.DecryptAesGcm(_key, b.Nonce, b.Ciphertext, b.Tag, ad);
                    return true;
                }

                if (Version == V3)
                {
                    byte[] adTitle = BlockAuth.BuildAssociatedData(V3, b, AD_PURPOSE_TITLE);
                    byte[] titlePt = Crypto.DecryptAesGcm(_key, b.TitleNonce, b.TitleCiphertext, b.TitleTag, adTitle);
                    b.Title = Encoding.UTF8.GetString(titlePt);

                    byte[] adBody = BlockAuth.BuildAssociatedData(V3, b, AD_PURPOSE_BODY);
                    _ = Crypto.DecryptAesGcm(_key, b.Nonce, b.Ciphertext, b.Tag, adBody);

                    return true;
                }

                // V4: only validate/hydrate if we have BEK
                if (b.BlockKey == null || (b.MyRights & BlockRights.Read) == 0)
                {
                    b.Title = "<restricted>";
                    return true;
                }

                byte[] adTitleV4 = BlockAuth.BuildAssociatedData(V4, b, AD_PURPOSE_TITLE);
                byte[] titlePtV4 = Crypto.DecryptAesGcm(b.BlockKey, b.TitleNonce, b.TitleCiphertext, b.TitleTag, adTitleV4);
                b.Title = Encoding.UTF8.GetString(titlePtV4);

                byte[] adBodyV4 = BlockAuth.BuildAssociatedData(V4, b, AD_PURPOSE_BODY);
                _ = Crypto.DecryptAesGcm(b.BlockKey, b.Nonce, b.Ciphertext, b.Tag, adBodyV4);

                return true;
            }
            catch
            {
                error = $"Authenticity check failed at block {b.Index} (Tag mismatch).";
                return false;
            }
        }

        // ============================================================
        // Internal chain + crypto (V4)
        // ============================================================

        private void EnsureWritableFrom(int startIndex)
        {
            if (Version != V4)
                return;

            if (_blocks.Count == 0) return;
            if (startIndex < 0) startIndex = 0;
            if (startIndex >= _blocks.Count) return;

            for (int i = startIndex; i < _blocks.Count; i++)
            {
                var b = _blocks[i];
                if (b.BlockKey == null || (b.MyRights & BlockRights.Write) == 0)
                    throw new UnauthorizedAccessException("Operation requires write access to this block and all subsequent blocks (hash chain).");
            }
        }

        private void ReencryptFromV4(int startIndex)
        {
            if (_blocks.Count == 0) return;

            if (startIndex <= 0) startIndex = 1;
            if (startIndex >= _blocks.Count) return;

            EnsureWritableFrom(startIndex);

            // rolling hash of previous block (already consistent)
            byte[] prevHash = ComputeBlockHash(_blocks[startIndex - 1]);

            for (int i = startIndex; i < _blocks.Count; i++)
            {
                var cur = _blocks[i];

                if (cur.BlockKey == null)
                    throw new UnauthorizedAccessException("No access to block key (BEK).");

                // decrypt current plaintexts using OLD AD
                string titlePt = DecryptTitleV4OrThrow(cur);
                byte[] bodyPt = DecryptBodyV4OrThrow(cur);

                // update PrevHash
                cur.PrevHash = prevHash;

                // re-encrypt title/body with SAME BEK but NEW AD (PrevHash in AD)
                EncryptTitleIntoV4(cur, cur.BlockKey, titlePt);
                EncryptBodyIntoV4(cur, cur.BlockKey, bodyPt);

                // update rolling prevHash for next block
                prevHash = ComputeBlockHash(cur);
            }
        }

        private string DecryptTitleV4OrThrow(Block b)
        {
            if (b.BlockKey == null)
                throw new UnauthorizedAccessException("No access to block key (BEK).");

            byte[] adTitle = BlockAuth.BuildAssociatedData(V4, b, AD_PURPOSE_TITLE);
            byte[] titlePt = Crypto.DecryptAesGcm(b.BlockKey, b.TitleNonce, b.TitleCiphertext, b.TitleTag, adTitle);
            return Encoding.UTF8.GetString(titlePt);
        }

        private byte[] DecryptBodyV4OrThrow(Block b)
        {
            if (b.BlockKey == null)
                throw new UnauthorizedAccessException("No access to block key (BEK).");

            byte[] adBody = BlockAuth.BuildAssociatedData(V4, b, AD_PURPOSE_BODY);
            return Crypto.DecryptAesGcm(b.BlockKey, b.Nonce, b.Ciphertext, b.Tag, adBody);
        }

        private void EncryptTitleIntoV4(Block b, byte[] bek, string title)
        {
            byte[] titleBytes = Encoding.UTF8.GetBytes(title ?? string.Empty);
            byte[] ad = BlockAuth.BuildAssociatedData(V4, b, AD_PURPOSE_TITLE);
            var (nonce, ct, tag) = Crypto.EncryptAesGcm(bek, titleBytes, ad);

            b.TitleNonce = nonce;
            b.TitleCiphertext = ct;
            b.TitleTag = tag;
        }

        private void EncryptBodyIntoV4(Block b, byte[] bek, byte[] plaintext)
        {
            byte[] ad = BlockAuth.BuildAssociatedData(V4, b, AD_PURPOSE_BODY);
            var (nonce, ct, tag) = Crypto.EncryptAesGcm(bek, plaintext, ad);

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
                throw new InvalidOperationException("Invalid container key length in memory.");
        }

        public void CloseKeyMaterial()
        {
            if (_key.Length > 0)
            {
                Array.Clear(_key, 0, _key.Length);
                _key = Array.Empty<byte>();
            }

            foreach (var b in _blocks)
            {
                if (b.BlockKey != null && b.BlockKey.Length > 0)
                    Array.Clear(b.BlockKey, 0, b.BlockKey.Length);
                b.BlockKey = null;
            }
        }
    }

    public sealed class RecipientEntry
    {
        public byte[] KeyId { get; set; } = Array.Empty<byte>();      // 32 bytes SHA-256(SPKI)
        public byte[] PublicKeySpki { get; set; } = Array.Empty<byte>(); // V4 only (required in V4)
        public byte Alg { get; set; }                                  // 1 = RSA-OAEP-SHA256
        public byte[] WrappedDek { get; set; } = Array.Empty<byte>();  // RSA-encrypted 32-byte container DEK
    }
}
