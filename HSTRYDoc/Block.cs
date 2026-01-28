// Block.cs (supports V2/V3/V4 in-memory)
// V2 fields used: Title (plaintext), Nonce/Tag/Ciphertext (body)
// V3 fields used: TitleNonce/TitleTag/TitleCiphertext + Nonce/Tag/Ciphertext (body)
// V4 fields used: KeySlots + TitleNonce/TitleTag/TitleCiphertext + Nonce/Tag/Ciphertext (body)
// Title is plaintext in memory (hydrated during Validate for V3/V4), not stored plaintext on disk in V3/V4.

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
        public byte[] KeyId { get; set; } = Array.Empty<byte>();       // 32 bytes SHA256(SPKI)
        public BlockRights Rights { get; set; } = BlockRights.None;
        public byte Alg { get; set; }                                  // 1 = RSA-OAEP-SHA256
        public byte[] WrappedBek { get; set; } = Array.Empty<byte>();  // RSA-encrypted 32-byte BEK
    }

    public sealed class Block
    {
        public int Index { get; internal set; }

        // Plaintext title in memory only (hydrated on Validate for V3/V4)
        public string Title { get; internal set; } = string.Empty;

        public DateTimeOffset CreatedUtc { get; internal set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset ModifiedUtc { get; internal set; } = DateTimeOffset.UtcNow;

        public byte[] PrevHash { get; internal set; } = new byte[Crypto.Sha256Size];

        // V4: Per-block access control
        public List<BlockKeySlot> KeySlots { get; } = new();

        // In-memory only (not stored):
        internal byte[]? BlockKey { get; set; } = null;     // decrypted BEK for active user (if any)
        internal BlockRights MyRights { get; set; } = BlockRights.None;

        // V3/V4: AES-GCM payload for title
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
