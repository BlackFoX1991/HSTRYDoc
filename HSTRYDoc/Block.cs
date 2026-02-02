// Block.cs (V5 in-memory)
// - V5 fields used:
//   - KeySlots + TitleNonce/TitleTag/TitleCiphertext + Nonce/Tag/Ciphertext
// - Per-block access control via BEK (Block Encryption Key, 32 bytes) and KeySlots.
// - KeySlots store: KeyId (SHA256(ECDH SPKI), 32 bytes) + Rights + Alg + WrappedBek(envelope).
// - Alg:
//   - 1 = ECDH(P-256) + HKDF-SHA256 + AES-GCM key-wrap envelope (for BEK)

using System;
using System.Collections.Generic;

namespace HSTRYDoc
{
    [Flags]
    public enum BlockRights : byte
    {
        None = 0,
        Read = 1,
        Write = 2,
    }

    public sealed class BlockKeySlot
    {
        // 32 bytes = SHA-256(ECDH SPKI)
        public byte[] KeyId { get; set; } = Array.Empty<byte>();

        public BlockRights Rights { get; set; } = BlockRights.None;

        // 1 = ECDH-HKDF-SHA256-AESGCM envelope
        public byte Alg { get; set; }

        // Wrapped 32-byte BEK as an envelope blob (format depends on Alg)
        public byte[] WrappedBek { get; set; } = Array.Empty<byte>();
    }

    public sealed class Block
    {
        public int Index { get; internal set; }

        // Plaintext title in memory only (hydrated during Validate() when readable)
        public string Title { get; internal set; } = string.Empty;

        public DateTimeOffset CreatedUtc { get; internal set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset ModifiedUtc { get; internal set; } = DateTimeOffset.UtcNow;

        // 32 bytes SHA-256 hash chain (same as before)
        public byte[] PrevHash { get; internal set; } = new byte[Crypto.Sha256Size];

        // Per-block access control (V5)
        public List<BlockKeySlot> KeySlots { get; } = new();

        // In-memory only (not stored):
        internal byte[]? BlockKey { get; set; } = null; // decrypted BEK for active user (32 bytes)
        internal BlockRights MyRights { get; set; } = BlockRights.None;

        // AES-GCM payload for title
        public byte[] TitleNonce { get; internal set; } = Array.Empty<byte>();      // 12 bytes
        public byte[] TitleTag { get; internal set; } = Array.Empty<byte>();        // 16 bytes
        public byte[] TitleCiphertext { get; internal set; } = Array.Empty<byte>(); // n bytes

        // AES-GCM payload for body
        public byte[] Nonce { get; internal set; } = Array.Empty<byte>();      // 12 bytes
        public byte[] Tag { get; internal set; } = Array.Empty<byte>();        // 16 bytes
        public byte[] Ciphertext { get; internal set; } = Array.Empty<byte>(); // n bytes

        public int StoredSizeBytes
        {
            get
            {
                int a = Ciphertext?.Length ?? 0;
                int t = TitleCiphertext?.Length ?? 0;
                return a + t;
            }
        }
    }
}
