// BlockAuth.cs
// V7 AD (access-controlled): version + purpose + index + created + modified + prevHash + accessHash(32)

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
            // Hard-upgrade: V7 only
            if (containerVersion != 7)
                throw new InvalidOperationException("Unsupported container version for AD.");

            // V7: version + purpose + index + created + modified + prevHash + accessHash
            byte[] accessHash = ComputeAccessHashV7(b);

            int len = 1 + 1 + 4 + 8 + 8 + Crypto.Sha256Size + Crypto.Sha256Size;

            byte[] ad = new byte[len];
            int o = 0;

            ad[o++] = containerVersion;
            ad[o++] = purpose;

            BinaryPrimitives.WriteInt32LittleEndian(ad.AsSpan(o, 4), b.Index);
            o += 4;

            BinaryPrimitives.WriteInt64LittleEndian(ad.AsSpan(o, 8), b.CreatedUtc.UtcTicks);
            o += 8;

            BinaryPrimitives.WriteInt64LittleEndian(ad.AsSpan(o, 8), b.ModifiedUtc.UtcTicks);
            o += 8;

            b.PrevHash.AsSpan().CopyTo(ad.AsSpan(o, Crypto.Sha256Size));
            o += Crypto.Sha256Size;

            accessHash.AsSpan().CopyTo(ad.AsSpan(o, Crypto.Sha256Size));
            return ad;
        }

        // Access metadata binding (still same structure as V4, but now used by V7)
        public static byte[] ComputeAccessHashV7(Block b)
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
