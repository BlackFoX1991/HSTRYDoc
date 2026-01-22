// Container.cs
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace HSTRYDoc
{
    /// <summary>
    /// Secure, editable blockchain-like container.
    /// - Each block stores encrypted RTF bytes using AES-GCM.
    /// - Associated Data includes chain + metadata (Option B, max security).
    /// - Any change to a block forces re-encryption of that block and re-encryption of all following blocks (because PrevHash changes).
    /// </summary>
    public sealed class HSTRYContainer
    {
        public byte Version { get; private set; } = Global.ContainerVersion;

        public int Iterations { get; private set; } = 300_000;
        public byte[] Salt { get; private set; } = Array.Empty<byte>();
        public byte[] KeyCheck { get; private set; } = Array.Empty<byte>();

        public string EncodingWebName { get; private set; } = Global.CurrentEditorEncoding.WebName;

        private readonly List<Block> _blocks = new();
        public IReadOnlyList<Block> Blocks => _blocks;

        // derived key held in memory while container is open
        private byte[] _key = Array.Empty<byte>();

        private HSTRYContainer() { }

        public static HSTRYContainer CreateNew(string password, int iterations = 300_000, Encoding? encoding = null)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password must not be empty.");

            var c = new HSTRYContainer
            {
                Version = Global.ContainerVersion,
                Iterations = iterations,
                Salt = Crypto.RandomBytes(Crypto.SaltSize),
            };

            var enc = encoding ?? Global.CurrentEditorEncoding;
            c.EncodingWebName = enc.WebName;

            c._key = Crypto.DeriveKeyPbkdf2(password, c.Salt, c.Iterations, Crypto.KeySize);
            c.KeyCheck = Crypto.ComputeKeyCheck(c._key);

            return c;
        }

        public static HSTRYContainer Load(string path, string password)
        {
            using var fs = File.OpenRead(path);
            return Load(fs, password);
        }

        public static HSTRYContainer Load(Stream stream, string password)
        {
            using var br = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

            byte[] magic = br.ReadBytes(Global.FileMagic.Length);
            if (!magic.SequenceEqual(Global.FileMagic))
                throw new InvalidDataException("Invalid file format (magic mismatch).");

            var c = new HSTRYContainer
            {
                Version = br.ReadByte(),
                Iterations = br.ReadInt32()
            };

            int saltLen = br.ReadByte();
            c.Salt = br.ReadBytes(saltLen);

            int keyCheckLen = br.ReadByte();
            c.KeyCheck = br.ReadBytes(keyCheckLen);

            int encNameLen = br.ReadByte();
            c.EncodingWebName = Encoding.UTF8.GetString(br.ReadBytes(encNameLen));

            c._key = Crypto.DeriveKeyPbkdf2(password, c.Salt, c.Iterations, Crypto.KeySize);
            byte[] computedCheck = Crypto.ComputeKeyCheck(c._key);

            if (!Crypto.FixedTimeEquals(computedCheck, c.KeyCheck))
                throw new CryptographicException("Wrong password.");

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

            // Validate chain + AEAD tags
            if (!c.Validate(out string error))
                throw new InvalidDataException($"Container invalid: {error}");

            return c;
        }

        public void Save(string path)
        {
            using var fs = File.Create(path);
            Save(fs);
        }

        public void Save(Stream stream)
        {
            EnsureKey();

            using var bw = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

            bw.Write(Global.FileMagic);
            bw.Write(Version);
            bw.Write(Iterations);

            bw.Write((byte)Salt.Length);
            bw.Write(Salt);

            bw.Write((byte)KeyCheck.Length);
            bw.Write(KeyCheck);

            byte[] encBytes = Encoding.UTF8.GetBytes(EncodingWebName);
            bw.Write((byte)encBytes.Length);
            bw.Write(encBytes);

            bw.Write(_blocks.Count);

            foreach (var b in _blocks)
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
        }

        public long GetStoredSizeBytes()
        {
            long sum = 0;
            foreach (var b in _blocks)
                sum += b.StoredSizeBytes;
            return sum;
        }

        public string GenerateUniqueTitle()
        {
            // Random and practically non-duplicating. Also verify uniqueness within container.
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

            // chain changed -> re-encrypt following blocks (Option B)
            ReencryptFrom(index + 1);
        }

        public void RenameBlock(int index, string newTitle)
        {
            EnsureKey();

            if (string.IsNullOrWhiteSpace(newTitle))
                throw new ArgumentException("Title must not be empty.");

            // avoid duplicates
            if (_blocks.Any(x => x.Index != index && string.Equals(x.Title, newTitle, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("A block with the same title already exists.");

            var b = GetBlock(index);

            // Need plaintext to re-encrypt with new AD.
            byte[] oldAd = BlockAuth.BuildAssociatedData(Version, b);
            byte[] pt = Crypto.DecryptAesGcm(_key, b.Nonce, b.Ciphertext, b.Tag, oldAd);

            b.Title = newTitle;
            b.ModifiedUtc = DateTimeOffset.UtcNow;

            EncryptInto(b, pt);

            ReencryptFrom(index + 1);
        }

        public byte[] ComputeBlockHash(Block b)
        {
            // Deterministic hash over persistent fields:
            // version(1) + index(4) + createdTicks(8) + modifiedTicks(8) + prevHash(32) +
            // titleLen(2) + title(utf8) + nonce(12) + tag(16) + ctLen(4) + ct(n)
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

                // AEAD check: attempt to decrypt (no need to keep plaintext)
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

        private void ReencryptFrom(int startIndex)
        {
            if (startIndex <= 0) startIndex = 1;

            for (int i = startIndex; i < _blocks.Count; i++)
            {
                var prev = _blocks[i - 1];
                var cur = _blocks[i];

                // 1) decrypt with OLD AD (cur.PrevHash is still old)
                byte[] oldAd = BlockAuth.BuildAssociatedData(Version, cur);
                byte[] pt = Crypto.DecryptAesGcm(_key, cur.Nonce, cur.Ciphertext, cur.Tag, oldAd);

                // 2) set new PrevHash based on previous block's NEW hash
                cur.PrevHash = ComputeBlockHash(prev);

                // 3) re-encrypt with NEW AD (PrevHash is part of AD) - keep ModifiedUtc as-is (structural change only)
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
}
