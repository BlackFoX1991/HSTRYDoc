// Container.cs (V7 only, ECC P-384 + ContainerId binding)
// - Only supports V7 (no migration, no V2/V3/V4/V5 loading).
// - Header:
//   - OwnerSigningPublicKeySpki: ECDSA P-384 public key (SPKI)
//   - OwnerEcdhPublicKeySpki: ECDH P-384 public key (SPKI)
//   - ContainerId: 32 bytes random, signed, used for key-wrap binding
//   - Recipients: ECDH public SPKI + KeyId=SHA256(SPKI) + WrappedDek envelope
//   - HeaderSignature: ECDSA(SHA-256) over header-signing data
// - Key wrapping (DEK/BEK):
//   - ECDH ephem (P-384) -> shared secret -> HKDF-SHA256 -> KEK(32) -> AES-GCM wrap
//   - HKDF/AD bind purpose + recipient KeyId + containerId + epk hash
// - Data encryption (blocks):
//   - AES-GCM with per-block BEK (32 bytes)
// - Access control AD binding and block chain remain as before (V4 logic).

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace HSTRYDoc
{
    public sealed class HSTRYContainer
    {

        public bool IsOpenedAsOwner
    => _activeKeyId != null && _activeKeyId.Length == 32 && _activeKeyId.SequenceEqual(OwnerKeyId);

        // =========================
        // V7 constants
        // =========================
        public const byte CurrentVersion = 7;
        private const byte V7 = 7;

        // Recipient algs (V7)
        private const byte RECIPIENT_ALG_ECDH_HKDF_SHA256_AESGCM = 1;

        // Header signature algs (V7)
        private const byte HEADER_SIGALG_ECDSA_P384_SHA256 = 1;

        // Block AEAD purpose bytes
        private const byte AD_PURPOSE_TITLE = 1;
        private const byte AD_PURPOSE_BODY = 2;

        // Wrap purposes (for KeyWrap AD/info separation)
        private const byte WRAP_PURPOSE_DEK = 10;
        private const byte WRAP_PURPOSE_BEK = 11;

        // ContainerId
        private const int ContainerIdSize = 32;

        // =========================
        // Public properties
        // =========================
        public byte Version { get; private set; } = CurrentVersion;
        public string EncodingWebName { get; private set; } = Global.CurrentEditorEncoding.WebName;

        // Owner keys (V7)
        public byte[] OwnerSigningPublicKeySpki { get; private set; } = Array.Empty<byte>(); // ECDSA pub (P-384)
        public byte[] OwnerEcdhPublicKeySpki { get; private set; } = Array.Empty<byte>();    // ECDH pub (P-384)

        // Stable container binding
        public byte[] ContainerId { get; private set; } = Array.Empty<byte>(); // 32 bytes

        // Owner identity for access control = SHA256(OwnerEcdhPublicKeySpki)
        public byte[] OwnerKeyId => (OwnerEcdhPublicKeySpki.Length == 0) ? Array.Empty<byte>() : SHA256.HashData(OwnerEcdhPublicKeySpki);

        public byte HeaderSignatureAlg { get; private set; } = HEADER_SIGALG_ECDSA_P384_SHA256;
        public byte[] HeaderSignature { get; private set; } = Array.Empty<byte>();

        private readonly List<RecipientEntry> _recipients = new();
        public IReadOnlyList<RecipientEntry> Recipients => _recipients;

        private readonly List<Block> _blocks = new();
        public IReadOnlyList<Block> Blocks => _blocks;

        // Container DEK (32 bytes, AES-256) in memory while open
        private byte[] _key = Array.Empty<byte>();

        // Active user's KeyId = SHA256(ECDH SPKI derived from private key)
        private byte[] _activeKeyId = Array.Empty<byte>();

        // Cached encoding
        private Encoding? _encCache;
        private static readonly byte[] ZeroHash = new byte[Crypto.Sha256Size];

        private HSTRYContainer() { }

        public static HSTRYContainer LoadWithPrivateKeyFile(string containerPath, ECDiffieHellman myEcdhPrivateKey)
        {
            using var fs = new FileStream(containerPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1 << 20);
            using var bs = new BufferedStream(fs, 1 << 20);
            return LoadWithPrivateKey(bs, myEcdhPrivateKey);
        }


        private Encoding GetContainerEncoding()
            => _encCache ??= Encoding.GetEncoding(EncodingWebName);

        // ============================================================
        // Key files (ECDH + ECDSA) - P-384
        // ============================================================
        public static class EcdhKeyFiles
        {

            public static void SavePrivateKeyPkcs8Encrypted(string path, ECDiffieHellman ecdh, string password)
            {
                if (string.IsNullOrEmpty(password))
                    throw new ArgumentException("Password required.", nameof(password));

                var pbe = new PbeParameters(
                    PbeEncryptionAlgorithm.Aes256Cbc,
                    HashAlgorithmName.SHA256,
                    iterationCount: 300_000);

                byte[] enc = ecdh.ExportEncryptedPkcs8PrivateKey(password, pbe);
                File.WriteAllBytes(path, enc); // binary
            }

            public static ECDiffieHellman LoadPrivateKeyPkcs8Encrypted(string path, string password)
            {
                if (string.IsNullOrEmpty(password))
                    throw new ArgumentException("Password required.", nameof(password));

                byte[] enc = File.ReadAllBytes(path);

                var e = ECDiffieHellman.Create();
                e.ImportEncryptedPkcs8PrivateKey(password, enc, out _);
                return e;
            }

            // Public key file: Base64(SPKI)
            public static void SavePublicKeySpki(string path, ECDiffieHellman ecdh)
            {
                byte[] spki = ecdh.ExportSubjectPublicKeyInfo();
                File.WriteAllText(path, Convert.ToBase64String(spki), Encoding.ASCII);
            }

            public static ECDiffieHellman LoadPublicKeySpki(string path)
            {
                string b64 = File.ReadAllText(path, Encoding.ASCII).Trim();
                byte[] spki = Convert.FromBase64String(b64);

                var e = ECDiffieHellman.Create();
                e.ImportSubjectPublicKeyInfo(spki, out _);
                return e;
            }

            // KeyId = SHA256(SPKI)
            public static byte[] ComputeKeyIdFromPublicKey(ECDiffieHellman ecdh)
            {
                byte[] spki = ecdh.ExportSubjectPublicKeyInfo();
                return SHA256.HashData(spki);
            }

            public static ECDiffieHellman CreateNewKeyPair()
            {
                return ECDiffieHellman.Create(ECCurve.NamedCurves.nistP384);
            }
        }

        public static class EcdsaKeyFiles
        {

            public static void SavePrivateKeyPkcs8Encrypted(string path, ECDsa ecdsa, string password)
            {
                if (string.IsNullOrEmpty(password))
                    throw new ArgumentException("Password required.", nameof(password));

                var pbe = new PbeParameters(
                    PbeEncryptionAlgorithm.Aes256Cbc,
                    HashAlgorithmName.SHA256,
                    iterationCount: 300_000);

                byte[] enc = ecdsa.ExportEncryptedPkcs8PrivateKey(password, pbe);
                File.WriteAllBytes(path, enc); // binary
            }

            public static ECDsa LoadPrivateKeyPkcs8Encrypted(string path, string password)
            {
                if (string.IsNullOrEmpty(password))
                    throw new ArgumentException("Password required.", nameof(password));

                byte[] enc = File.ReadAllBytes(path);

                var s = ECDsa.Create();
                s.ImportEncryptedPkcs8PrivateKey(password, enc, out _);
                return s;
            }

            public static void SavePublicKeySpki(string path, ECDsa ecdsa)
            {
                byte[] spki = ecdsa.ExportSubjectPublicKeyInfo();
                File.WriteAllText(path, Convert.ToBase64String(spki), Encoding.ASCII);
            }

            public static ECDsa LoadPublicKeySpki(string path)
            {
                string b64 = File.ReadAllText(path, Encoding.ASCII).Trim();
                byte[] spki = Convert.FromBase64String(b64);

                var s = ECDsa.Create();
                s.ImportSubjectPublicKeyInfo(spki, out _);
                return s;
            }

            public static ECDsa CreateNewKeyPair()
            {
                return ECDsa.Create(ECCurve.NamedCurves.nistP384);
            }
        }



        // ============================================================
        // Create / Load (V7 only)
        // ============================================================

        /// <summary>
        /// Create a new V7 container.
        /// Owner uses ECDSA for header signing and ECDH for membership + block BEK ownership.
        /// Recipients are ECDH public keys (should include owner ECDH; will be ensured).
        /// </summary>
        public static HSTRYContainer CreateNewForRecipients(
    ECDsa ownerSigningPrivateKey,
    ECDiffieHellman ownerEcdhPrivateKey,
    IEnumerable<ECDiffieHellman> recipientEcdhPublicKeys,
    Encoding? encoding = null)
        {
            if (ownerSigningPrivateKey == null) throw new ArgumentNullException(nameof(ownerSigningPrivateKey));
            if (ownerEcdhPrivateKey == null) throw new ArgumentNullException(nameof(ownerEcdhPrivateKey));
            if (recipientEcdhPublicKeys == null) throw new ArgumentNullException(nameof(recipientEcdhPublicKeys));

            var pubs = recipientEcdhPublicKeys.ToList();

            // Ensure owner is included as recipient (membership)
            byte[] ownerEcdhSpki = ownerEcdhPrivateKey.ExportSubjectPublicKeyInfo();
            byte[] ownerKid = SHA256.HashData(ownerEcdhSpki);

            bool ownerAlready = pubs.Any(p =>
            {
                byte[] spki = p.ExportSubjectPublicKeyInfo();
                return SHA256.HashData(spki).SequenceEqual(ownerKid);
            });

            if (!ownerAlready)
                pubs.Insert(0, ownerEcdhPrivateKey);

            if (pubs.Count == 0)
                throw new ArgumentException("At least one recipient public key is required.");

            var c = new HSTRYContainer
            {
                Version = CurrentVersion
            };

            var enc = encoding ?? Global.CurrentEditorEncoding;
            c.EncodingWebName = enc.WebName;
            c._encCache = null;

            c.OwnerSigningPublicKeySpki = ownerSigningPrivateKey.ExportSubjectPublicKeyInfo();
            c.OwnerEcdhPublicKeySpki = ownerEcdhSpki;

            // Stable container binding
            c.ContainerId = new byte[ContainerIdSize];
            RandomNumberGenerator.Fill(c.ContainerId);

            // active identity = owner
            c._activeKeyId = ownerKid;

            // DEK (32 bytes)
            c._key = new byte[32];
            RandomNumberGenerator.Fill(c._key);

            // Recipients with wrapped DEK (SigPub optional: initially empty)
            foreach (var pub in pubs)
            {
                byte[] spki = pub.ExportSubjectPublicKeyInfo();
                byte[] keyId = SHA256.HashData(spki);

                byte[] wrappedDek = EccKeyWrap.WrapKey32(
                    key32: c._key,
                    recipientEcdhSpki: spki,
                    recipientKeyId: keyId,
                    purpose: WRAP_PURPOSE_DEK,
                    containerId: c.ContainerId);

                c._recipients.Add(new RecipientEntry
                {
                    KeyId = keyId,
                    PublicKeySpki = spki,
                    Alg = RECIPIENT_ALG_ECDH_HKDF_SHA256_AESGCM,
                    WrappedDek = wrappedDek,
                    SigningPublicKeySpki = Array.Empty<byte>() // V7: optional, empty here
                });
            }

            // Ensure unique KeyIds
            if (c._recipients.Select(r => Convert.ToHexString(r.KeyId))
                .Distinct(StringComparer.OrdinalIgnoreCase).Count() != c._recipients.Count)
                throw new InvalidOperationException("Duplicate recipient keys detected (KeyId collision).");

            // Sign header
            c.ResignHeader(ownerSigningPrivateKey);

            return c;
        }

        public static HSTRYContainer LoadWithPrivateKey(Stream stream, ECDiffieHellman myEcdhPrivateKey)
        {
            if (myEcdhPrivateKey == null) throw new ArgumentNullException(nameof(myEcdhPrivateKey));

            using var br = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

            byte[] magic = br.ReadBytes(Global.FileMagic.Length);
            if (!magic.SequenceEqual(Global.FileMagic))
                throw new InvalidDataException("Invalid file format (magic mismatch).");

            byte version = br.ReadByte();
            if (version != CurrentVersion)
                throw new InvalidDataException($"Unsupported container version: {version}.");

            var c = new HSTRYContainer { Version = version };

            int encNameLen = br.ReadByte();
            c.EncodingWebName = Encoding.UTF8.GetString(br.ReadBytes(encNameLen));
            c._encCache = null;

            ushort ownerSignLen = br.ReadUInt16();
            c.OwnerSigningPublicKeySpki = br.ReadBytes(ownerSignLen);

            ushort ownerEcdhLen = br.ReadUInt16();
            c.OwnerEcdhPublicKeySpki = br.ReadBytes(ownerEcdhLen);

            c.ContainerId = br.ReadBytes(ContainerIdSize);
            if (c.ContainerId.Length != ContainerIdSize)
                throw new InvalidDataException("ContainerId missing/invalid.");

            int recipientCount = br.ReadInt32();
            if (recipientCount <= 0)
                throw new InvalidDataException("Recipient list is empty.");

            for (int i = 0; i < recipientCount; i++)
            {
                int keyIdLen = br.ReadByte();
                byte[] keyId = br.ReadBytes(keyIdLen);

                ushort spkiLen = br.ReadUInt16();
                if (spkiLen == 0) throw new InvalidDataException("Recipient SPKI missing (V7).");
                byte[] spki = br.ReadBytes(spkiLen);

                byte[] check = SHA256.HashData(spki);
                if (!check.SequenceEqual(keyId))
                    throw new InvalidDataException("Recipient KeyId does not match SPKI (V7).");

                byte alg = br.ReadByte();

                ushort wrappedLen = br.ReadUInt16();
                byte[] wrappedDek = br.ReadBytes(wrappedLen);

                // V7: optional signing SPKI (u16 len + bytes)
                ushort sigLen = br.ReadUInt16();
                byte[] sigSpki = (sigLen == 0) ? Array.Empty<byte>() : br.ReadBytes(sigLen);
                if (sigLen != 0 && sigSpki.Length != sigLen)
                    throw new InvalidDataException("Recipient signing SPKI truncated.");

                if (sigLen != 0)
                {
                    using var tmp = ECDsa.Create();
                    tmp.ImportSubjectPublicKeyInfo(sigSpki, out _);
                }

                c._recipients.Add(new RecipientEntry
                {
                    KeyId = keyId,
                    PublicKeySpki = spki,
                    Alg = alg,
                    WrappedDek = wrappedDek,
                    SigningPublicKeySpki = sigSpki
                });
            }

            c.HeaderSignatureAlg = br.ReadByte();
            ushort sigHdrLen = br.ReadUInt16();
            c.HeaderSignature = br.ReadBytes(sigHdrLen);

            c.VerifyHeaderSignatureOrThrow();

            c._activeKeyId = EcdhKeyFiles.ComputeKeyIdFromPublicKey(myEcdhPrivateKey);

            var entry = c._recipients.FirstOrDefault(r => r.KeyId.SequenceEqual(c._activeKeyId));
            if (entry == null)
                throw new CryptographicException("No matching recipient entry for this private key.");

            if (entry.Alg != RECIPIENT_ALG_ECDH_HKDF_SHA256_AESGCM)
                throw new CryptographicException("Unsupported recipient key algorithm.");

            c._key = EccKeyWrap.UnwrapKey32(
                myPrivateEcdh: myEcdhPrivateKey,
                envelope: entry.WrappedDek,
                myKeyId: c._activeKeyId,
                purpose: WRAP_PURPOSE_DEK,
                containerId: c.ContainerId);

            int blockCount = br.ReadInt32();
            for (int i = 0; i < blockCount; i++)
            {
                var b = new Block { Index = br.ReadInt32() };

                b.CreatedUtc = new DateTimeOffset(br.ReadInt64(), TimeSpan.Zero);
                b.ModifiedUtc = new DateTimeOffset(br.ReadInt64(), TimeSpan.Zero);

                b.PrevHash = br.ReadBytes(Crypto.Sha256Size);

                int slotCount = br.ReadInt32();
                if (slotCount < 0) throw new InvalidDataException("Invalid KeySlot count (V7).");

                for (int s = 0; s < slotCount; s++)
                {
                    int kidLen = br.ReadByte();
                    byte[] kid = br.ReadBytes(kidLen);

                    var rights = (BlockRights)br.ReadByte();
                    byte alg2 = br.ReadByte();

                    ushort wlen = br.ReadUInt16();
                    byte[] wrappedBek = br.ReadBytes(wlen);

                    b.KeySlots.Add(new BlockKeySlot
                    {
                        KeyId = kid,
                        Rights = rights,
                        Alg = alg2,
                        WrappedBek = wrappedBek
                    });
                }

                var mySlot = b.KeySlots.FirstOrDefault(x => x.KeyId.SequenceEqual(c._activeKeyId));
                if (mySlot != null)
                {
                    if (mySlot.Alg != RECIPIENT_ALG_ECDH_HKDF_SHA256_AESGCM)
                        throw new CryptographicException("Unsupported block key algorithm.");

                    byte[] bek = EccKeyWrap.UnwrapKey32(
                        myPrivateEcdh: myEcdhPrivateKey,
                        envelope: mySlot.WrappedBek,
                        myKeyId: c._activeKeyId,
                        purpose: WRAP_PURPOSE_BEK,
                        containerId: c.ContainerId);

                    if (bek.Length != 32) throw new CryptographicException("Invalid BEK length.");
                    b.BlockKey = bek;
                    b.MyRights = mySlot.Rights;
                }

                b.TitleNonce = br.ReadBytes(Crypto.AesGcmNonceSize);
                b.TitleTag = br.ReadBytes(Crypto.AesGcmTagSize);

                int titleCtLen = br.ReadInt32();
                if (titleCtLen < 0) throw new InvalidDataException("Invalid title ciphertext length.");
                b.TitleCiphertext = br.ReadBytes(titleCtLen);

                b.Nonce = br.ReadBytes(Crypto.AesGcmNonceSize);
                b.Tag = br.ReadBytes(Crypto.AesGcmTagSize);

                int ctLen = br.ReadInt32();
                if (ctLen < 0) throw new InvalidDataException("Invalid ciphertext length.");
                b.Ciphertext = br.ReadBytes(ctLen);

                b.Title = string.Empty;
                c._blocks.Add(b);
            }

            if (!c.Validate(out string error))
                throw new InvalidDataException($"Container invalid: {error}");

            return c;
        }

        // ============================================================
        // Save (V7 only)
        // ============================================================
        public void Save(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("A valid target path is required.", nameof(path));

            string fullPath = Path.GetFullPath(path);
            string? directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            string tempPath = CreateTemporarySavePath(fullPath);
            string recoveryPath = GetRecoveryFilePath(fullPath);

            try
            {
                using (var fs = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 1 << 20))
                using (var bs = new BufferedStream(fs, 1 << 20))
                {
                    Save(bs);
                    bs.Flush();
                    fs.Flush(flushToDisk: true);
                }

                if (File.Exists(fullPath))
                    File.Replace(tempPath, fullPath, recoveryPath, ignoreMetadataErrors: true);
                else
                    File.Move(tempPath, fullPath);
            }
            catch
            {
                TryDeleteFile(tempPath);
                throw;
            }
        }

        public void Save(Stream stream)
        {
            EnsureKey();
            EnsureHeaderSigned();
            EnsureOwnerRecipientPresent();

            using var bw = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

            bw.Write(Global.FileMagic);
            bw.Write(Version);

            byte[] encBytes = Encoding.UTF8.GetBytes(EncodingWebName);
            bw.Write((byte)encBytes.Length);
            bw.Write(encBytes);

            if (OwnerSigningPublicKeySpki.Length == 0) throw new InvalidDataException("Owner signing public key is missing.");
            if (OwnerEcdhPublicKeySpki.Length == 0) throw new InvalidDataException("Owner ECDH public key is missing.");

            if (OwnerSigningPublicKeySpki.Length > ushort.MaxValue) throw new InvalidDataException("Owner signing public key is too large.");
            if (OwnerEcdhPublicKeySpki.Length > ushort.MaxValue) throw new InvalidDataException("Owner ECDH public key is too large.");

            bw.Write((ushort)OwnerSigningPublicKeySpki.Length);
            bw.Write(OwnerSigningPublicKeySpki);

            bw.Write((ushort)OwnerEcdhPublicKeySpki.Length);
            bw.Write(OwnerEcdhPublicKeySpki);

            if (ContainerId == null || ContainerId.Length != ContainerIdSize)
                throw new InvalidDataException("ContainerId missing/invalid.");

            bw.Write(ContainerId);

            bw.Write(_recipients.Count);
            foreach (var r in _recipients)
            {
                if (r.KeyId == null || r.KeyId.Length == 0) throw new InvalidDataException("Recipient KeyId missing.");
                if (r.PublicKeySpki == null || r.PublicKeySpki.Length == 0) throw new InvalidDataException("Recipient SPKI missing (V7).");
                if (r.WrappedDek == null || r.WrappedDek.Length == 0) throw new InvalidDataException("Recipient WrappedDek missing.");

                bw.Write((byte)r.KeyId.Length);
                bw.Write(r.KeyId);

                if (r.PublicKeySpki.Length > ushort.MaxValue) throw new InvalidDataException("Recipient SPKI too large.");
                bw.Write((ushort)r.PublicKeySpki.Length);
                bw.Write(r.PublicKeySpki);

                bw.Write(r.Alg);

                if (r.WrappedDek.Length > ushort.MaxValue) throw new InvalidDataException("WrappedDek too large.");
                bw.Write((ushort)r.WrappedDek.Length);
                bw.Write(r.WrappedDek);

                // V7: optional signing public key SPKI
                byte[] sig = r.SigningPublicKeySpki ?? Array.Empty<byte>();
                if (sig.Length > ushort.MaxValue) throw new InvalidDataException("Recipient signing SPKI too large.");
                bw.Write((ushort)sig.Length);
                if (sig.Length > 0)
                    bw.Write(sig);
            }

            bw.Write(HeaderSignatureAlg);

            if (HeaderSignature.Length > ushort.MaxValue)
                throw new InvalidDataException("Header signature too large.");

            bw.Write((ushort)HeaderSignature.Length);
            bw.Write(HeaderSignature);

            bw.Write(_blocks.Count);
            foreach (var b in _blocks)
                WriteBlockV7(bw, b); // Block format bleibt wie bei dir (falls du es auch V7 nennen willst: WriteBlockV7)
        }

        private static void WriteBlockV7(BinaryWriter bw, Block b)
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
                throw new InvalidDataException("Encrypted title is missing (V7).");
            if (b.TitleNonce == null || b.TitleNonce.Length != Crypto.AesGcmNonceSize)
                throw new InvalidDataException("Title nonce missing/invalid (V7).");
            if (b.TitleTag == null || b.TitleTag.Length != Crypto.AesGcmTagSize)
                throw new InvalidDataException("Title tag missing/invalid (V7).");

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

        internal static string GetRecoveryFilePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("A valid path is required.", nameof(path));

            return Path.GetFullPath(path) + ".recovery";
        }

        private static string CreateTemporarySavePath(string fullPath)
        {
            string directory = Path.GetDirectoryName(fullPath) ?? Environment.CurrentDirectory;
            string fileName = Path.GetFileName(fullPath);
            return Path.Combine(directory, $".{fileName}.{Guid.NewGuid():N}.tmp");
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // ignore cleanup failures
            }
        }

        // ============================================================
        // Recipient management (header signed by owner ECDSA)
        // ============================================================
        public void AddRecipient(
     ECDsa ownerSigningPrivateKey,
     ECDiffieHellman recipientEcdhPublicKey,
     byte[]? recipientSigningPublicKeySpki = null)
        {
            EnsureKey();
            EnsureOwnerSigningPrivateKeyMatches(ownerSigningPrivateKey);

            byte[] spki = recipientEcdhPublicKey.ExportSubjectPublicKeyInfo();
            byte[] keyId = SHA256.HashData(spki);

            if (_recipients.Any(r => r.KeyId.SequenceEqual(keyId)))
                throw new InvalidOperationException("Recipient already exists.");

            byte[] wrappedDek = EccKeyWrap.WrapKey32(_key, spki, keyId, WRAP_PURPOSE_DEK, ContainerId);

            byte[] sigSpki = Array.Empty<byte>();
            if (recipientSigningPublicKeySpki != null && recipientSigningPublicKeySpki.Length > 0)
            {
                // Validate parse (optional but prevents garbage)
                using var tmp = ECDsa.Create();
                tmp.ImportSubjectPublicKeyInfo(recipientSigningPublicKeySpki, out _);

                sigSpki = recipientSigningPublicKeySpki.ToArray();
            }

            _recipients.Add(new RecipientEntry
            {
                KeyId = keyId,
                PublicKeySpki = spki,
                Alg = RECIPIENT_ALG_ECDH_HKDF_SHA256_AESGCM,
                WrappedDek = wrappedDek,
                SigningPublicKeySpki = sigSpki
            });

            ResignHeader(ownerSigningPrivateKey);
        }

        public bool RemoveRecipientByKeyIdHex(ECDsa ownerSigningPrivateKey, string keyIdHex)
        {
            EnsureKey();
            EnsureOwnerSigningPrivateKeyMatches(ownerSigningPrivateKey);

            if (string.IsNullOrWhiteSpace(keyIdHex))
                return false;

            byte[] keyId;
            try { keyId = Convert.FromHexString(keyIdHex); }
            catch { return false; }

            if (keyId.SequenceEqual(OwnerKeyId))
                throw new InvalidOperationException("The owner recipient entry cannot be removed. Transfer ownership first if needed.");

            int removed = _recipients.RemoveAll(r => r.KeyId.SequenceEqual(keyId));
            if (removed > 0)
                ResignHeader(ownerSigningPrivateKey);

            return removed > 0;
        }



        // ============================================================
        // Block access control (V7) - owner must provide ECDH private key
        // ============================================================
        public void GrantBlockAccess(ECDiffieHellman ownerEcdhPrivateKey, int blockIndex, ECDiffieHellman recipientEcdhPublicKey, BlockRights rights, bool replaceExisting = true)
        {
            EnsureKey();
            EnsureOwnerEcdhPrivateKeyMatches(ownerEcdhPrivateKey);

            if ((uint)blockIndex >= (uint)_blocks.Count)
                throw new ArgumentOutOfRangeException(nameof(blockIndex));
            if (recipientEcdhPublicKey == null)
                throw new ArgumentNullException(nameof(recipientEcdhPublicKey));

            byte[] spki = recipientEcdhPublicKey.ExportSubjectPublicKeyInfo();
            byte[] keyId = SHA256.HashData(spki);

            if (!_recipients.Any(r => r.KeyId.SequenceEqual(keyId)))
                throw new InvalidOperationException("Recipient is not in container recipients. AddRecipient(...) first.");

            var b = _blocks[blockIndex];
            EnsureWritableFrom(blockIndex);

            if (b.BlockKey == null || b.BlockKey.Length != 32)
                throw new UnauthorizedAccessException("No access to block key (BEK).");

            string titlePt = DecryptTitleV7OrThrow(b);
            byte[] bodyPt = DecryptBodyV7OrThrow(b);

            if (replaceExisting)
                b.KeySlots.RemoveAll(s => s.KeyId.SequenceEqual(keyId));

            byte[] wrappedBek = EccKeyWrap.WrapKey32(b.BlockKey, spki, keyId, WRAP_PURPOSE_BEK, ContainerId);

            b.KeySlots.Add(new BlockKeySlot
            {
                KeyId = keyId,
                Rights = rights,
                Alg = RECIPIENT_ALG_ECDH_HKDF_SHA256_AESGCM,
                WrappedBek = wrappedBek
            });

            EncryptTitleIntoV7(b, b.BlockKey, titlePt);
            EncryptBodyIntoV7(b, b.BlockKey, bodyPt);

            ReencryptFromV7(blockIndex + 1);
        }

        public void RevokeBlockAccess(ECDiffieHellman ownerEcdhPrivateKey, int blockIndex, string keyIdHex)
        {
            EnsureKey();
            EnsureOwnerEcdhPrivateKeyMatches(ownerEcdhPrivateKey);

            if ((uint)blockIndex >= (uint)_blocks.Count)
                throw new ArgumentOutOfRangeException(nameof(blockIndex));

            if (string.IsNullOrWhiteSpace(keyIdHex))
                throw new ArgumentException("KeyId is required.");

            byte[] keyId = Convert.FromHexString(keyIdHex);

            var b = _blocks[blockIndex];
            EnsureWritableFrom(blockIndex);

            if (b.BlockKey == null || b.BlockKey.Length != 32)
                throw new UnauthorizedAccessException("No access to block key (BEK).");

            string titlePt = DecryptTitleV7OrThrow(b);
            byte[] bodyPt = DecryptBodyV7OrThrow(b);

            int removed = b.KeySlots.RemoveAll(s => s.KeyId.SequenceEqual(keyId));
            if (removed == 0)
                return;

            EncryptTitleIntoV7(b, b.BlockKey, titlePt);
            EncryptBodyIntoV7(b, b.BlockKey, bodyPt);

            ReencryptFromV7(blockIndex + 1);
        }

        // ============================================================
        // Bulk access (V7)
        // ============================================================
        public void GrantReadAllBlocks(ECDiffieHellman ownerEcdhPrivateKey, ECDiffieHellman recipientEcdhPublicKey, IProgress<UiProgress> progress, CancellationToken token)
        {
            BulkSetAccessAllBlocks(ownerEcdhPrivateKey, recipientEcdhPublicKey, recipientPublicKeyKeyId: null,
                mode: BulkAccessMode.GrantReadIfMissing, rights: BlockRights.Read, progress: progress, token: token);
        }

        public void GrantWriteAllBlocks(ECDiffieHellman ownerEcdhPrivateKey, ECDiffieHellman recipientEcdhPublicKey, IProgress<UiProgress> progress, CancellationToken token)
        {
            BulkSetAccessAllBlocks(ownerEcdhPrivateKey, recipientEcdhPublicKey, recipientPublicKeyKeyId: null,
                mode: BulkAccessMode.GrantOverwrite, rights: (BlockRights.Read | BlockRights.Write), progress: progress, token: token);
        }

        public void RevokeAllBlocks(ECDiffieHellman ownerEcdhPrivateKey, byte[] recipientKeyId, IProgress<UiProgress> progress, CancellationToken token)
        {
            if (recipientKeyId == null || recipientKeyId.Length == 0)
                throw new ArgumentException("recipientKeyId is required.", nameof(recipientKeyId));

            BulkSetAccessAllBlocks(ownerEcdhPrivateKey, recipientPublicKey: null, recipientPublicKeyKeyId: recipientKeyId,
                mode: BulkAccessMode.Revoke, rights: BlockRights.None, progress: progress, token: token);
        }

        private enum BulkAccessMode
        {
            GrantReadIfMissing,
            GrantOverwrite,
            Revoke
        }

        private void BulkSetAccessAllBlocks(
            ECDiffieHellman ownerEcdhPrivateKey,
            ECDiffieHellman? recipientPublicKey,
            byte[]? recipientPublicKeyKeyId,
            BulkAccessMode mode,
            BlockRights rights,
            IProgress<UiProgress> progress,
            CancellationToken token)
        {
            EnsureKey();
            EnsureOwnerEcdhPrivateKeyMatches(ownerEcdhPrivateKey);

            int n = _blocks.Count;
            if (n == 0) return;

            byte[] recipientKeyId;
            byte[] recipientSpki = Array.Empty<byte>();

            if (recipientPublicKey != null)
            {
                recipientSpki = recipientPublicKey.ExportSubjectPublicKeyInfo();
                recipientKeyId = SHA256.HashData(recipientSpki);
            }
            else
            {
                recipientKeyId = recipientPublicKeyKeyId!;
            }

            if (mode != BulkAccessMode.Revoke && !_recipients.Any(r => r.KeyId.SequenceEqual(recipientKeyId)))
                throw new InvalidOperationException("Recipient is not in container recipients. AddRecipient(...) first.");

            var titles = new string[n];
            var bodies = new byte[n][];
            var beks = new byte[n][];

            progress.Report(new UiProgress { Message = "Decrypting blocks…", Indeterminate = false, Maximum = n, Value = 0 });

            for (int i = 0; i < n; i++)
            {
                token.ThrowIfCancellationRequested();

                var b = _blocks[i];

                byte[] bek = b.BlockKey ?? Array.Empty<byte>();
                if (bek.Length != 32)
                {
                    var ownerSlot = b.KeySlots.FirstOrDefault(s => s.KeyId.SequenceEqual(OwnerKeyId));
                    if (ownerSlot == null)
                        throw new CryptographicException($"Owner KeySlot missing at block {i}.");

                    if (ownerSlot.Alg != RECIPIENT_ALG_ECDH_HKDF_SHA256_AESGCM)
                        throw new CryptographicException("Unsupported block key algorithm.");

                    bek = EccKeyWrap.UnwrapKey32(ownerEcdhPrivateKey, ownerSlot.WrappedBek, OwnerKeyId, WRAP_PURPOSE_BEK, ContainerId);
                    if (bek.Length != 32)
                        throw new CryptographicException("Invalid BEK length.");

                    b.BlockKey = bek;
                    b.MyRights = BlockRights.Read | BlockRights.Write;
                }

                titles[i] = DecryptTitleV7OrThrow(b);
                bodies[i] = DecryptBodyV7OrThrow(b);
                beks[i] = bek;

                progress.Report(new UiProgress { Message = $"Decrypting blocks… {i + 1}/{n}", Indeterminate = false, Maximum = n, Value = i + 1 });
            }

            progress.Report(new UiProgress { Message = "Updating access lists…", Indeterminate = false, Maximum = n, Value = 0 });

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
                            progress.Report(new UiProgress { Message = $"Updating access lists… {i + 1}/{n}", Maximum = n, Value = i + 1, Indeterminate = false });
                            continue;
                        }
                    }

                    if (existing != null)
                        b.KeySlots.Remove(existing);

                    byte[] wrappedBek = EccKeyWrap.WrapKey32(beks[i], recipientSpki, recipientKeyId, WRAP_PURPOSE_BEK, ContainerId);

                    b.KeySlots.Add(new BlockKeySlot
                    {
                        KeyId = recipientKeyId,
                        Rights = rights,
                        Alg = RECIPIENT_ALG_ECDH_HKDF_SHA256_AESGCM,
                        WrappedBek = wrappedBek
                    });
                }

                progress.Report(new UiProgress { Message = $"Updating access lists… {i + 1}/{n}", Indeterminate = false, Maximum = n, Value = i + 1 });
            }

            progress.Report(new UiProgress { Message = "Rebuilding chain…", Indeterminate = false, Maximum = n, Value = 0 });

            if (_blocks.Count > 0)
                _blocks[0].PrevHash = ZeroHash.ToArray();

            for (int i = 0; i < n; i++)
            {
                token.ThrowIfCancellationRequested();

                var b = _blocks[i];
                b.Index = i;

                if (i == 0)
                    b.PrevHash = ZeroHash.ToArray();
                else
                    b.PrevHash = ComputeBlockHash(_blocks[i - 1]);

                b.Title = titles[i];

                EncryptTitleIntoV7(b, beks[i], titles[i]);
                EncryptBodyIntoV7(b, beks[i], bodies[i]);

                progress.Report(new UiProgress { Message = $"Rebuilding chain… {i + 1}/{n}", Indeterminate = false, Maximum = n, Value = i + 1 });
            }

            for (int i = 0; i < n; i++)
            {
                bodies[i] = Array.Empty<byte>();
                titles[i] = string.Empty;
                beks[i] = Array.Empty<byte>();
            }
        }

        // ============================================================
        // Blocks API (V7)
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

            if (!IsOpenedAsOwner)
                throw new UnauthorizedAccessException("Only the container owner can create new blocks.");

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

            b.BlockKey = new byte[32];
            RandomNumberGenerator.Fill(b.BlockKey);

            b.KeySlots.Clear();
            foreach (var r in _recipients)
            {
                var rights = r.KeyId.SequenceEqual(OwnerKeyId)
                    ? (BlockRights.Read | BlockRights.Write)
                    : BlockRights.None;

                byte[] wrappedBek = EccKeyWrap.WrapKey32(b.BlockKey, r.PublicKeySpki, r.KeyId, WRAP_PURPOSE_BEK, ContainerId);

                b.KeySlots.Add(new BlockKeySlot
                {
                    KeyId = r.KeyId,
                    Rights = rights,
                    Alg = RECIPIENT_ALG_ECDH_HKDF_SHA256_AESGCM,
                    WrappedBek = wrappedBek
                });

                if (r.KeyId.SequenceEqual(_activeKeyId))
                    b.MyRights = rights;
            }

            EncryptTitleIntoV7(b, b.BlockKey, title);

            byte[] plaintext = GetContainerEncoding().GetBytes(rtf ?? string.Empty);
            EncryptBodyIntoV7(b, b.BlockKey, plaintext);

            _blocks.Add(b);
            return b;
        }

        public string GetRtfDocument(int index)
        {
            EnsureKey();
            var b = GetBlock(index);

            if ((b.MyRights & BlockRights.Read) == 0 || b.BlockKey == null)
                throw new UnauthorizedAccessException("No read access to this block.");

            byte[] adBody = BlockAuth.BuildAssociatedData(V7, b, AD_PURPOSE_BODY);
            byte[] bodyPt = Crypto.DecryptAesGcm(b.BlockKey, b.Nonce, b.Ciphertext, b.Tag, adBody);
            return GetContainerEncoding().GetString(bodyPt);
        }

        public void UpdateRtfDocument(int index, string newRtf)
        {
            EnsureKey();
            var b = GetBlock(index);

            EnsureWritableFrom(index);

            if (b.BlockKey == null)
                throw new UnauthorizedAccessException("No access to this block.");

            DateTimeOffset oldModified = b.ModifiedUtc;

            string oldTitle;
            try
            {
                oldTitle = DecryptTitleV7OrThrow(b);
                _ = DecryptBodyV7OrThrow(b);
            }
            catch
            {
                throw;
            }

            try
            {
                b.ModifiedUtc = DateTimeOffset.UtcNow;

                EncryptTitleIntoV7(b, b.BlockKey, oldTitle);

                byte[] pt = GetContainerEncoding().GetBytes(newRtf ?? string.Empty);
                EncryptBodyIntoV7(b, b.BlockKey, pt);

                ReencryptFromV7(index + 1);
            }
            catch
            {
                b.ModifiedUtc = oldModified;
                throw;
            }
        }

        public void RemoveBlock(int index)
        {
            EnsureKey();

            if ((uint)index >= (uint)_blocks.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            EnsureWritableFrom(index);

            int affectedCount = _blocks.Count - (index + 1);
            string[] trailingTitles = new string[affectedCount];
            byte[][] trailingBodies = new byte[affectedCount][];

            for (int i = index + 1; i < _blocks.Count; i++)
            {
                var block = _blocks[i];
                trailingTitles[i - index - 1] = DecryptTitleV7OrThrow(block);
                trailingBodies[i - index - 1] = DecryptBodyV7OrThrow(block);
            }

            _blocks.RemoveAt(index);

            if (_blocks.Count == 0)
                return;

            for (int i = index; i < _blocks.Count; i++)
            {
                var block = _blocks[i];
                block.Index = i;
                block.PrevHash = i == 0 ? ZeroHash.ToArray() : ComputeBlockHash(_blocks[i - 1]);

                string title = trailingTitles[i - index];
                byte[] body = trailingBodies[i - index];

                EncryptTitleIntoV7(block, block.BlockKey!, title);
                EncryptBodyIntoV7(block, block.BlockKey!, body);
            }
        }

        public void RenameBlock(int index, string newTitle)
        {
            EnsureKey();

            if (string.IsNullOrWhiteSpace(newTitle))
                throw new ArgumentException("Title must not be empty.");

            if (_blocks.Any(x => x.Index != index && string.Equals(x.Title, newTitle, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("A block with the same title already exists.");

            var b = GetBlock(index);
            EnsureWritableFrom(index);

            if (b.BlockKey == null)
                throw new UnauthorizedAccessException("No access to this block.");

            byte[] oldAdBody = BlockAuth.BuildAssociatedData(V7, b, AD_PURPOSE_BODY);
            byte[] bodyPt = Crypto.DecryptAesGcm(b.BlockKey, b.Nonce, b.Ciphertext, b.Tag, oldAdBody);

            b.Title = newTitle;
            b.ModifiedUtc = DateTimeOffset.UtcNow;

            EncryptTitleIntoV7(b, b.BlockKey, b.Title);
            EncryptBodyIntoV7(b, b.BlockKey, bodyPt);

            ReencryptFromV7(index + 1);
        }

        // ============================================================
        // Ownership transfer (V7) - FIXED old owner slot removal
        // ============================================================
        public void TransferOwnership(
            ECDsa currentOwnerSigningPrivateKey,
            ECDiffieHellman currentOwnerEcdhPrivateKey,
            ECDsa newOwnerSigningPrivateKey,
            ECDiffieHellman newOwnerEcdhPrivateKey,
            IProgress<UiProgress>? progress = null,
            CancellationToken token = default)
        {
            EnsureKey();
            EnsureOwnerSigningPrivateKeyMatches(currentOwnerSigningPrivateKey);
            EnsureOwnerEcdhPrivateKeyMatches(currentOwnerEcdhPrivateKey);

            byte[] oldOwnerKid = SHA256.HashData(OwnerEcdhPublicKeySpki);

            byte[] newOwnerSignSpki = newOwnerSigningPrivateKey.ExportSubjectPublicKeyInfo();
            byte[] newOwnerEcdhSpki = newOwnerEcdhPrivateKey.ExportSubjectPublicKeyInfo();
            byte[] newOwnerKid = SHA256.HashData(newOwnerEcdhSpki);

            if (!_recipients.Any(r => r.KeyId.SequenceEqual(newOwnerKid)))
            {
                byte[] wrappedDek = EccKeyWrap.WrapKey32(_key, newOwnerEcdhSpki, newOwnerKid, WRAP_PURPOSE_DEK, ContainerId);
                _recipients.Add(new RecipientEntry
                {
                    KeyId = newOwnerKid,
                    PublicKeySpki = newOwnerEcdhSpki,
                    Alg = RECIPIENT_ALG_ECDH_HKDF_SHA256_AESGCM,
                    WrappedDek = wrappedDek
                });
            }

            EnsureWritableFrom(0);

            int n = _blocks.Count;
            progress?.Report(new UiProgress { Message = "Transferring ownership…", Indeterminate = false, Maximum = n, Value = 0 });

            var titles = new string[n];
            var bodies = new byte[n][];
            var beks = new byte[n][];

            for (int i = 0; i < n; i++)
            {
                token.ThrowIfCancellationRequested();

                var b = _blocks[i];

                if (b.BlockKey == null || b.BlockKey.Length != 32)
                {
                    var ownerSlot = b.KeySlots.FirstOrDefault(s => s.KeyId.SequenceEqual(oldOwnerKid));
                    if (ownerSlot == null)
                        throw new CryptographicException($"Owner KeySlot missing at block {i}.");

                    b.BlockKey = EccKeyWrap.UnwrapKey32(currentOwnerEcdhPrivateKey, ownerSlot.WrappedBek, oldOwnerKid, WRAP_PURPOSE_BEK, ContainerId);
                    b.MyRights = BlockRights.Read | BlockRights.Write;
                }

                titles[i] = DecryptTitleV7OrThrow(b);
                bodies[i] = DecryptBodyV7OrThrow(b);
                beks[i] = b.BlockKey;

                progress?.Report(new UiProgress { Message = $"Transferring ownership… {i + 1}/{n}", Indeterminate = false, Maximum = n, Value = i + 1 });
            }

            // Update header owner keys
            OwnerSigningPublicKeySpki = newOwnerSignSpki;
            OwnerEcdhPublicKeySpki = newOwnerEcdhSpki;

            // Update slots in all blocks: remove old owner slot, ensure new owner slot
            for (int i = 0; i < n; i++)
            {
                token.ThrowIfCancellationRequested();
                var b = _blocks[i];

                b.KeySlots.RemoveAll(s => s.KeyId.SequenceEqual(oldOwnerKid));
                b.KeySlots.RemoveAll(s => s.KeyId.SequenceEqual(newOwnerKid)); // avoid duplicates

                byte[] wrappedBek = EccKeyWrap.WrapKey32(beks[i], newOwnerEcdhSpki, newOwnerKid, WRAP_PURPOSE_BEK, ContainerId);
                b.KeySlots.Add(new BlockKeySlot
                {
                    KeyId = newOwnerKid,
                    Rights = BlockRights.Read | BlockRights.Write,
                    Alg = RECIPIENT_ALG_ECDH_HKDF_SHA256_AESGCM,
                    WrappedBek = wrappedBek
                });
            }

            // resign header with NEW owner's signing key
            ResignHeader(newOwnerSigningPrivateKey);

            // rebuild chain (accessHash changed -> AD changed -> re-encrypt all)
            if (n > 0)
                _blocks[0].PrevHash = ZeroHash.ToArray();

            for (int i = 0; i < n; i++)
            {
                token.ThrowIfCancellationRequested();

                var b = _blocks[i];
                b.Index = i;

                if (i == 0) b.PrevHash = ZeroHash.ToArray();
                else b.PrevHash = ComputeBlockHash(_blocks[i - 1]);

                b.Title = titles[i];

                EncryptTitleIntoV7(b, beks[i], titles[i]);
                EncryptBodyIntoV7(b, beks[i], bodies[i]);
            }

            _activeKeyId = newOwnerKid;
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

            // Title payload
            Span<byte> tlen = stackalloc byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(tlen, b.TitleCiphertext?.Length ?? 0);

            ih.AppendData(b.TitleNonce);
            ih.AppendData(b.TitleTag);
            ih.AppendData(tlen);
            if (b.TitleCiphertext != null && b.TitleCiphertext.Length > 0)
                ih.AppendData(b.TitleCiphertext);

            // V7: AccessHash
            byte[] accessHash = BlockAuth.ComputeAccessHashV7(b);
            ih.AppendData(accessHash);

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
                if (b.BlockKey == null || (b.MyRights & BlockRights.Read) == 0)
                {
                    b.Title = "<restricted>";
                    return true;
                }

                byte[] adTitle = BlockAuth.BuildAssociatedData(V7, b, AD_PURPOSE_TITLE);
                byte[] titlePt = Crypto.DecryptAesGcm(b.BlockKey, b.TitleNonce, b.TitleCiphertext, b.TitleTag, adTitle);
                b.Title = Encoding.UTF8.GetString(titlePt);

                byte[] adBody = BlockAuth.BuildAssociatedData(V7, b, AD_PURPOSE_BODY);
                _ = Crypto.DecryptAesGcm(b.BlockKey, b.Nonce, b.Ciphertext, b.Tag, adBody);

                return true;
            }
            catch
            {
                error = $"Authenticity check failed at block {b.Index} (Tag mismatch).";
                return false;
            }
        }

        // ============================================================
        // Internal chain + crypto (V7)
        // ============================================================
        private void EnsureWritableFrom(int startIndex)
        {
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

        private void ReencryptFromV7(int startIndex)
        {
            if (_blocks.Count == 0) return;

            if (startIndex < 0) startIndex = 0;
            if (startIndex >= _blocks.Count) return;

            EnsureWritableFrom(startIndex);

            byte[] prevHash = startIndex == 0
                ? ZeroHash.ToArray()
                : ComputeBlockHash(_blocks[startIndex - 1]);

            for (int i = startIndex; i < _blocks.Count; i++)
            {
                var cur = _blocks[i];

                if (cur.BlockKey == null)
                    throw new UnauthorizedAccessException("No access to block key (BEK).");

                string titlePt = DecryptTitleV7OrThrow(cur);
                byte[] bodyPt = DecryptBodyV7OrThrow(cur);

                cur.PrevHash = i == 0 ? ZeroHash.ToArray() : prevHash;

                EncryptTitleIntoV7(cur, cur.BlockKey, titlePt);
                EncryptBodyIntoV7(cur, cur.BlockKey, bodyPt);

                prevHash = ComputeBlockHash(cur);
            }
        }

        private string DecryptTitleV7OrThrow(Block b)
        {
            if (b.BlockKey == null)
                throw new UnauthorizedAccessException("No access to block key (BEK).");

            byte[] adTitle = BlockAuth.BuildAssociatedData(V7, b, AD_PURPOSE_TITLE);
            byte[] titlePt = Crypto.DecryptAesGcm(b.BlockKey, b.TitleNonce, b.TitleCiphertext, b.TitleTag, adTitle);
            return Encoding.UTF8.GetString(titlePt);
        }

        private byte[] DecryptBodyV7OrThrow(Block b)
        {
            if (b.BlockKey == null)
                throw new UnauthorizedAccessException("No access to block key (BEK).");

            byte[] adBody = BlockAuth.BuildAssociatedData(V7, b, AD_PURPOSE_BODY);
            return Crypto.DecryptAesGcm(b.BlockKey, b.Nonce, b.Ciphertext, b.Tag, adBody);
        }

        private void EncryptTitleIntoV7(Block b, byte[] bek, string title)
        {
            byte[] titleBytes = Encoding.UTF8.GetBytes(title ?? string.Empty);
            byte[] ad = BlockAuth.BuildAssociatedData(V7, b, AD_PURPOSE_TITLE);
            var (nonce, ct, tag) = Crypto.EncryptAesGcm(bek, titleBytes, ad);

            b.TitleNonce = nonce;
            b.TitleCiphertext = ct;
            b.TitleTag = tag;
        }

        private void EncryptBodyIntoV7(Block b, byte[] bek, byte[] plaintext)
        {
            byte[] ad = BlockAuth.BuildAssociatedData(V7, b, AD_PURPOSE_BODY);
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
            if (Version != V7)
                throw new InvalidOperationException("Invalid container version in memory.");
            if (ContainerId == null || ContainerId.Length != ContainerIdSize)
                throw new InvalidOperationException("ContainerId missing/invalid in memory.");
        }

        private void EnsureOwnerRecipientPresent()
        {
            if (!_recipients.Any(r => r.KeyId.SequenceEqual(OwnerKeyId)))
                throw new InvalidOperationException("The owner recipient entry is missing.");
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

        // ============================================================
        // Header signing / verification (ECDSA)
        // ============================================================
        private void EnsureHeaderSigned()
        {
            if (HeaderSignature == null || HeaderSignature.Length == 0)
                throw new InvalidOperationException("Header is not signed.");
            if (HeaderSignatureAlg != HEADER_SIGALG_ECDSA_P384_SHA256)
                throw new InvalidOperationException("Unsupported header signature algorithm.");
        }

        private void EnsureOwnerSigningPrivateKeyMatches(ECDsa ownerSigningPrivateKey)
        {
            if (ownerSigningPrivateKey == null)
                throw new ArgumentNullException(nameof(ownerSigningPrivateKey));

            byte[] spki = ownerSigningPrivateKey.ExportSubjectPublicKeyInfo();
            if (!spki.SequenceEqual(OwnerSigningPublicKeySpki))
                throw new CryptographicException("The provided owner signing private key does not match the container owner signing public key.");
        }

        private void EnsureOwnerEcdhPrivateKeyMatches(ECDiffieHellman ownerEcdhPrivateKey)
        {
            if (ownerEcdhPrivateKey == null)
                throw new ArgumentNullException(nameof(ownerEcdhPrivateKey));

            byte[] spki = ownerEcdhPrivateKey.ExportSubjectPublicKeyInfo();
            if (!spki.SequenceEqual(OwnerEcdhPublicKeySpki))
                throw new CryptographicException("The provided owner ECDH private key does not match the container owner ECDH public key.");
        }

        private byte[] BuildHeaderSigningData()
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

            bw.Write(Version);

            byte[] encBytes = Encoding.UTF8.GetBytes(EncodingWebName);
            bw.Write((byte)encBytes.Length);
            bw.Write(encBytes);

            bw.Write((ushort)OwnerSigningPublicKeySpki.Length);
            bw.Write(OwnerSigningPublicKeySpki);

            bw.Write((ushort)OwnerEcdhPublicKeySpki.Length);
            bw.Write(OwnerEcdhPublicKeySpki);

            if (ContainerId == null || ContainerId.Length != ContainerIdSize)
                throw new InvalidOperationException("ContainerId missing/invalid.");

            bw.Write(ContainerId);

            bw.Write(_recipients.Count);
            foreach (var r in _recipients)
            {
                bw.Write((byte)r.KeyId.Length);
                bw.Write(r.KeyId);

                bw.Write((ushort)r.PublicKeySpki.Length);
                bw.Write(r.PublicKeySpki);

                bw.Write(r.Alg);

                bw.Write((ushort)r.WrappedDek.Length);
                bw.Write(r.WrappedDek);

                byte[] sig = r.SigningPublicKeySpki ?? Array.Empty<byte>();
                if (sig.Length > ushort.MaxValue) throw new InvalidOperationException("Recipient signing SPKI too large.");
                bw.Write((ushort)sig.Length);
                if (sig.Length > 0)
                    bw.Write(sig);
            }

            bw.Flush();
            return ms.ToArray();
        }

        private void ResignHeader(ECDsa ownerSigningPrivateKey)
        {
            EnsureOwnerSigningPrivateKeyMatches(ownerSigningPrivateKey);

            byte[] data = BuildHeaderSigningData();
            HeaderSignatureAlg = HEADER_SIGALG_ECDSA_P384_SHA256;
            HeaderSignature = ownerSigningPrivateKey.SignData(data, HashAlgorithmName.SHA256);
        }

        private void VerifyHeaderSignatureOrThrow()
        {
            if (OwnerSigningPublicKeySpki == null || OwnerSigningPublicKeySpki.Length == 0)
                throw new InvalidDataException("Owner signing public key missing.");

            if (HeaderSignature == null || HeaderSignature.Length == 0)
                throw new InvalidDataException("Header signature missing.");

            if (HeaderSignatureAlg != HEADER_SIGALG_ECDSA_P384_SHA256)
                throw new InvalidDataException("Unsupported header signature algorithm.");

            using var ownerPub = ECDsa.Create();
            ownerPub.ImportSubjectPublicKeyInfo(OwnerSigningPublicKeySpki, out _);

            byte[] data = BuildHeaderSigningData();

            bool ok = ownerPub.VerifyData(data, HeaderSignature, HashAlgorithmName.SHA256);
            if (!ok)
                throw new InvalidDataException("Header signature invalid. The file may have been tampered with.");
        }
    }

    public sealed class RecipientEntry
    {
        // 32 bytes SHA-256(SPKI)
        public byte[] KeyId { get; set; } = Array.Empty<byte>();

        // ECDH SPKI (recipient encryption / membership)
        public byte[] PublicKeySpki { get; set; } = Array.Empty<byte>();

        // 1 = ECDH-HKDF-SHA256-AESGCM
        public byte Alg { get; set; }

        // envelope for 32-byte DEK
        public byte[] WrappedDek { get; set; } = Array.Empty<byte>();

        // V7: OPTIONAL signing public key SPKI (ECDSA). Empty = not provided.
        public byte[] SigningPublicKeySpki { get; set; } = Array.Empty<byte>();
    }

    // ============================================================
    // ECC Key Wrap: ECDH(P-384) + HKDF-SHA256 + AES-GCM envelope
    // with ContainerId binding
    // ============================================================
    internal static class EccKeyWrap
    {
        private const int SaltSize = 32;
        private const int NonceSize = 12;
        private const int TagSize = 16;

        private static readonly byte[] InfoPrefix = Encoding.ASCII.GetBytes("HSTRY-KEYWRAP-V7");
        private static readonly byte[] AdPrefix = Encoding.ASCII.GetBytes("HSTRY-KEYWRAP-AD-V7");

        public static byte[] WrapKey32(byte[] key32, byte[] recipientEcdhSpki, byte[] recipientKeyId, byte purpose, byte[] containerId)
        {
            if (key32 == null || key32.Length != 32) throw new ArgumentException("Key must be 32 bytes.", nameof(key32));
            if (recipientKeyId == null || recipientKeyId.Length != 32) throw new ArgumentException("recipientKeyId must be 32 bytes.", nameof(recipientKeyId));
            if (containerId == null || containerId.Length != 32) throw new ArgumentException("containerId must be 32 bytes.", nameof(containerId));

            using var recipient = ECDiffieHellman.Create();
            recipient.ImportSubjectPublicKeyInfo(recipientEcdhSpki, out _);

            using var ephem = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP384);
            byte[] epkSpki = ephem.ExportSubjectPublicKeyInfo();

            byte[] ikm = ephem.DeriveKeyMaterial(recipient.PublicKey);

            byte[] salt = new byte[SaltSize];
            RandomNumberGenerator.Fill(salt);

            byte[] info = BuildInfo(purpose, recipientKeyId, containerId);
            byte[] kek = HkdfSha256(ikm, salt, info, 32);

            byte[] ad = BuildAd(purpose, recipientKeyId, epkSpki, containerId);

            var (nonce, ct, tag) = Crypto.EncryptAesGcm(kek, key32, ad);

            // Envelope:
            // u16 epkLen | epkSpki | salt(32) | nonce(12) | tag(16) | u16 ctLen | ct
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

            if (epkSpki.Length > ushort.MaxValue) throw new InvalidOperationException("Ephemeral SPKI too large.");
            bw.Write((ushort)epkSpki.Length);
            bw.Write(epkSpki);

            bw.Write(salt);
            bw.Write(nonce);
            bw.Write(tag);

            if (ct.Length > ushort.MaxValue) throw new InvalidOperationException("Ciphertext too large.");
            bw.Write((ushort)ct.Length);
            bw.Write(ct);

            bw.Flush();

            CryptographicOperations.ZeroMemory(ikm);
            CryptographicOperations.ZeroMemory(kek);

            return ms.ToArray();
        }

        public static byte[] UnwrapKey32(ECDiffieHellman myPrivateEcdh, byte[] envelope, byte[] myKeyId, byte purpose, byte[] containerId)
        {
            if (myPrivateEcdh == null) throw new ArgumentNullException(nameof(myPrivateEcdh));
            if (myKeyId == null || myKeyId.Length != 32) throw new ArgumentException("myKeyId must be 32 bytes.", nameof(myKeyId));
            if (containerId == null || containerId.Length != 32) throw new ArgumentException("containerId must be 32 bytes.", nameof(containerId));

            using var ms = new MemoryStream(envelope, writable: false);
            using var br = new BinaryReader(ms, Encoding.UTF8, leaveOpen: true);

            ushort epkLen = br.ReadUInt16();
            if (epkLen == 0) throw new CryptographicException("Invalid envelope (epkLen=0).");
            byte[] epkSpki = br.ReadBytes(epkLen);

            byte[] salt = br.ReadBytes(SaltSize);
            if (salt.Length != SaltSize) throw new CryptographicException("Invalid envelope (salt).");

            byte[] nonce = br.ReadBytes(NonceSize);
            if (nonce.Length != NonceSize) throw new CryptographicException("Invalid envelope (nonce).");

            byte[] tag = br.ReadBytes(TagSize);
            if (tag.Length != TagSize) throw new CryptographicException("Invalid envelope (tag).");

            ushort ctLen = br.ReadUInt16();
            if (ctLen == 0) throw new CryptographicException("Invalid envelope (ctLen=0).");
            byte[] ct = br.ReadBytes(ctLen);
            if (ct.Length != ctLen) throw new CryptographicException("Invalid envelope (ct).");

            using var epk = ECDiffieHellman.Create();
            epk.ImportSubjectPublicKeyInfo(epkSpki, out _);

            byte[] ikm = myPrivateEcdh.DeriveKeyMaterial(epk.PublicKey);

            byte[] info = BuildInfo(purpose, myKeyId, containerId);
            byte[] kek = HkdfSha256(ikm, salt, info, 32);

            byte[] ad = BuildAd(purpose, myKeyId, epkSpki, containerId);

            byte[] key32 = Crypto.DecryptAesGcm(kek, nonce, ct, tag, ad);
            if (key32 == null || key32.Length != 32) throw new CryptographicException("Invalid unwrapped key length.");

            CryptographicOperations.ZeroMemory(ikm);
            CryptographicOperations.ZeroMemory(kek);

            return key32;
        }

        private static byte[] BuildInfo(byte purpose, byte[] keyId, byte[] containerId)
        {
            byte[] info = new byte[InfoPrefix.Length + 1 + 32 + 32];
            Buffer.BlockCopy(InfoPrefix, 0, info, 0, InfoPrefix.Length);
            info[InfoPrefix.Length] = purpose;
            Buffer.BlockCopy(keyId, 0, info, InfoPrefix.Length + 1, 32);
            Buffer.BlockCopy(containerId, 0, info, InfoPrefix.Length + 1 + 32, 32);
            return info;
        }

        private static byte[] BuildAd(byte purpose, byte[] keyId, byte[] epkSpki, byte[] containerId)
        {
            // AD = "HSTRY-KEYWRAP-AD-V7" | purpose | keyId | SHA256(epkSpki) | containerId
            byte[] epkHash = SHA256.HashData(epkSpki);
            byte[] ad = new byte[AdPrefix.Length + 1 + 32 + 32 + 32];

            Buffer.BlockCopy(AdPrefix, 0, ad, 0, AdPrefix.Length);
            ad[AdPrefix.Length] = purpose;
            Buffer.BlockCopy(keyId, 0, ad, AdPrefix.Length + 1, 32);
            Buffer.BlockCopy(epkHash, 0, ad, AdPrefix.Length + 1 + 32, 32);
            Buffer.BlockCopy(containerId, 0, ad, AdPrefix.Length + 1 + 32 + 32, 32);

            return ad;
        }

        private static byte[] HkdfSha256(byte[] ikm, byte[] salt, byte[] info, int length)
        {
            byte[] prk;
            using (var h = new HMACSHA256(salt))
                prk = h.ComputeHash(ikm);

            const int HashLen = 32;
            int n = (length + HashLen - 1) / HashLen;
            if (n > 255) throw new ArgumentOutOfRangeException(nameof(length), "HKDF length too large.");

            byte[] okm = new byte[length];
            byte[] t = Array.Empty<byte>();
            int off = 0;

            for (int i = 1; i <= n; i++)
            {
                using var hmac = new HMACSHA256(prk);
                hmac.TransformBlock(t, 0, t.Length, null, 0);
                hmac.TransformBlock(info, 0, info.Length, null, 0);
                hmac.TransformFinalBlock(new[] { (byte)i }, 0, 1);

                t = hmac.Hash!;
                int toCopy = Math.Min(HashLen, length - off);
                Buffer.BlockCopy(t, 0, okm, off, toCopy);
                off += toCopy;
            }

            CryptographicOperations.ZeroMemory(prk);
            CryptographicOperations.ZeroMemory(t);
            return okm;
        }
    }
}
