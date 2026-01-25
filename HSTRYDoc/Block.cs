// Block.cs (supports V2 and V3 in-memory)
// V2 fields used: Title (plaintext), Nonce/Tag/Ciphertext (body)
// V3 fields used: TitleNonce/TitleTag/TitleCiphertext + Nonce/Tag/Ciphertext (body)
// Title is plaintext in memory (hydrated during Validate for V3), not stored plaintext on disk in V3.

using System;

namespace HSTRYDoc
{
    public sealed class Block
    {
        public int Index { get; internal set; }

        // Plaintext title in memory only
        public string Title { get; internal set; } = string.Empty;

        public DateTimeOffset CreatedUtc { get; internal set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset ModifiedUtc { get; internal set; } = DateTimeOffset.UtcNow;

        public byte[] PrevHash { get; internal set; } = new byte[Crypto.Sha256Size];

        // V3: AES-GCM payload for title
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
