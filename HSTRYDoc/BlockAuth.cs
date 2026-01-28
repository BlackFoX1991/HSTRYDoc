// BlockAuth.cs
// V2 AD (legacy): version + index + created + modified + prevHash + titleLen + title(utf8)
// V3 AD (current): version + purpose + index + created + modified + prevHash
// V4 AD (access-controlled): version + purpose + index + created + modified + prevHash + accessHash(32)
// Notes:
// - In V3/V4, title is encrypted, so it is NOT embedded plaintext in AD.
// - purpose separates title/body so ciphertexts cannot be swapped.
// - V4 accessHash binds KeySlots (KeyId + Rights + Alg + WrappedBek) into AEAD authentication,
//   preventing slot tampering without re-encryption.

using System;
using System.Buffers.Binary;
using System.IO;
using System.Security.Cryptography;
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

            if (containerVersion == 3)
            {
                // V3: version + purpose + index + created + modified + prevHash
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

            if (containerVersion != 4)
                throw new InvalidOperationException("Unsupported container version for AD.");

            // V4: version + purpose + index + created + modified + prevHash + accessHash
            byte[] accessHash = ComputeAccessHashV4(b);

            int lenV4 = 1 + 1 + 4 + 8 + 8 + Crypto.Sha256Size + Crypto.Sha256Size;

            byte[] adV4 = new byte[lenV4];
            int q = 0;

            adV4[q++] = containerVersion;
            adV4[q++] = purpose;

            BinaryPrimitives.WriteInt32LittleEndian(adV4.AsSpan(q, 4), b.Index);
            q += 4;

            BinaryPrimitives.WriteInt64LittleEndian(adV4.AsSpan(q, 8), b.CreatedUtc.UtcTicks);
            q += 8;

            BinaryPrimitives.WriteInt64LittleEndian(adV4.AsSpan(q, 8), b.ModifiedUtc.UtcTicks);
            q += 8;

            b.PrevHash.AsSpan().CopyTo(adV4.AsSpan(q, Crypto.Sha256Size));
            q += Crypto.Sha256Size;

            accessHash.AsSpan().CopyTo(adV4.AsSpan(q, Crypto.Sha256Size));
            return adV4;
        }

        public static byte[] ComputeAccessHashV4(Block b)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

            bw.Write(b.KeySlots.Count);
            foreach (var s in b.KeySlots)
            {
                bw.Write((byte)(s.KeyId?.Length ?? 0));
                if (s.KeyId != null && s.KeyId.Length > 0)
                    bw.Write(s.KeyId);

                bw.Write((byte)s.Rights);
                bw.Write(s.Alg);

                bw.Write((ushort)(s.WrappedBek?.Length ?? 0));
                if (s.WrappedBek != null && s.WrappedBek.Length > 0)
                    bw.Write(s.WrappedBek);
            }

            bw.Flush();
            return SHA256.HashData(ms.ToArray());
        }
    }
}
