// Block.cs
using System;

namespace HSTRYDoc
{
    public sealed class Block
    {
        public int Index { get; internal set; }

        public string Title { get; internal set; } = string.Empty;

        public DateTimeOffset CreatedUtc { get; internal set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset ModifiedUtc { get; internal set; } = DateTimeOffset.UtcNow;

        public byte[] PrevHash { get; internal set; } = new byte[Crypto.Sha256Size];

        // AES-GCM payload
        public byte[] Nonce { get; internal set; } = Array.Empty<byte>();      // 12 bytes
        public byte[] Tag { get; internal set; } = Array.Empty<byte>();        // 16 bytes
        public byte[] Ciphertext { get; internal set; } = Array.Empty<byte>(); // n bytes

        public int StoredSizeBytes => Ciphertext?.Length ?? 0;
    }
}
