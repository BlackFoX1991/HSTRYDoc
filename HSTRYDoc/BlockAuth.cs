// BlockAuth.cs
using System;
using System.Buffers.Binary;
using System.Text;

namespace HSTRYDoc
{
    internal static class BlockAuth
    {
        /// <summary>
        /// Build Associated Data (AD) for AES-GCM.
        /// Option B (max security): bind chain + metadata into AEAD authentication.
        /// Consequence: any change to Title/Created/Modified/PrevHash requires re-encrypt for that block.
        /// </summary>
        public static byte[] BuildAssociatedData(byte containerVersion, Block b)
        {
            // version(1) + index(4) + createdTicks(8) + modifiedTicks(8) + prevHash(32) + titleLen(2) + title(utf8)
            byte[] titleBytes = Encoding.UTF8.GetBytes(b.Title ?? string.Empty);
            int len = 1 + 4 + 8 + 8 + Crypto.Sha256Size + 2 + titleBytes.Length;

            byte[] ad = new byte[len];
            int o = 0;

            ad[o++] = containerVersion;

            BinaryPrimitives.WriteInt32LittleEndian(ad.AsSpan(o, 4), b.Index);
            o += 4;

            BinaryPrimitives.WriteInt64LittleEndian(ad.AsSpan(o, 8), b.CreatedUtc.UtcTicks);
            o += 8;

            BinaryPrimitives.WriteInt64LittleEndian(ad.AsSpan(o, 8), b.ModifiedUtc.UtcTicks);
            o += 8;

            b.PrevHash.AsSpan().CopyTo(ad.AsSpan(o, Crypto.Sha256Size));
            o += Crypto.Sha256Size;

            BinaryPrimitives.WriteUInt16LittleEndian(ad.AsSpan(o, 2), (ushort)titleBytes.Length);
            o += 2;

            titleBytes.CopyTo(ad.AsSpan(o));
            return ad;
        }
    }
}
