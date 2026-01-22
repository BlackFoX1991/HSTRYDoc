// Crypto.cs
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace HSTRYDoc
{
    internal static class Crypto
    {
        public const int Sha256Size = 32;
        public const int SaltSize = 16;
        public const int AesGcmNonceSize = 12;
        public const int AesGcmTagSize = 16;
        public const int KeySize = 32; // 256-bit

        public static byte[] RandomBytes(int len)
        {
            byte[] b = new byte[len];
            RandomNumberGenerator.Fill(b);
            return b;
        }

        public static byte[] DeriveKeyPbkdf2(string password, byte[] salt, int iterations, int keyBytes = KeySize)
        {
            return KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: iterations,
                numBytesRequested: keyBytes
            );
        }

        public static byte[] ComputeKeyCheck(byte[] key)
        {
            // Fast password check: SHA256(key || "HSTRYDoc-KeyCheck"), take 16 bytes.
            byte[] marker = Encoding.UTF8.GetBytes("HSTRYDoc-KeyCheck");
            byte[] buf = new byte[key.Length + marker.Length];
            Buffer.BlockCopy(key, 0, buf, 0, key.Length);
            Buffer.BlockCopy(marker, 0, buf, key.Length, marker.Length);

            byte[] hash = SHA256.HashData(buf);
            return hash.Take(16).ToArray();
        }

        public static (byte[] nonce, byte[] ciphertext, byte[] tag) EncryptAesGcm(byte[] key, byte[] plaintext, byte[] associatedData)
        {
            byte[] nonce = RandomBytes(AesGcmNonceSize);
            byte[] ciphertext = new byte[plaintext.Length];
            byte[] tag = new byte[AesGcmTagSize];

            using var gcm = new AesGcm(key, AesGcmTagSize);
            gcm.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);

            return (nonce, ciphertext, tag);
        }

        public static byte[] DecryptAesGcm(byte[] key, byte[] nonce, byte[] ciphertext, byte[] tag, byte[] associatedData)
        {
            byte[] plaintext = new byte[ciphertext.Length];
            using var gcm = new AesGcm(key, AesGcmTagSize);
            gcm.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);
            return plaintext;
        }

        public static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a is null || b is null) return false;
            return CryptographicOperations.FixedTimeEquals(a, b);
        }
    }
}
