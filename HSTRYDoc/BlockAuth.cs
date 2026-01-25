// BlockAuth.cs
// V2 AD (legacy): version + index + created + modified + prevHash + titleLen + title(utf8)
// V3 AD (current): version + purpose(1) + index + created + modified + prevHash
// Notes:
// - In V3, title is encrypted, so it is NOT embedded plaintext in AD.
// - purpose separates title/body so ciphertexts cannot be swapped.

using System;
using System.Buffers.Binary;
using System.Text;

namespace HSTRYDoc
{
    internal static class BlockAuth
    {
        public static byte[] BuildAssociatedData(byte containerVersion, Block b, byte purpose)
        {
            if (containerVersion == 2)
            {
                // Legacy V2: bind chain + metadata + plaintext title into AEAD authentication.
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

            // V3: version + purpose + index + created + modified + prevHash
            // This binds ordering/chain + timestamps, and separates title/body via purpose.
            int lenV3 = 1 + 1 + 4 + 8 + 8 + Crypto.Sha256Size;

            byte[] adV3 = new byte[lenV3];
            int p = 0;

            adV3[p++] = containerVersion;
            adV3[p++] = purpose;

            BinaryPrimitives.WriteInt32LittleEndian(adV3.AsSpan(p, 4), b.Index);
            p += 4;

            BinaryPrimitives.WriteInt64LittleEndian(adV3.AsSpan(p, 8), b.CreatedUtc.UtcTicks);
            p += 8;

            BinaryPrimitives.WriteInt64LittleEndian(adV3.AsSpan(p, 8), b.ModifiedUtc.UtcTicks);
            p += 8;

            b.PrevHash.AsSpan().CopyTo(adV3.AsSpan(p, Crypto.Sha256Size));
            return adV3;
        }
    }
}
