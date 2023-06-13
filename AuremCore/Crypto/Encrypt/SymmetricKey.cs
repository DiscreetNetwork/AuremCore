using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Crypto.Encrypt
{
    public class SymmetricKey
    {
        private AesGcm gcm;

        public SymmetricKey(byte[] key)
        {
            var hkey = new byte[32];
            SHA256.HashData(key, hkey);
            gcm = new AesGcm(hkey);
        }

        public byte[] Encrypt(byte[] msg)
        {
            var retv = new byte[AesGcm.NonceByteSizes.MaxSize + msg.Length + AesGcm.TagByteSizes.MaxSize];
            var nspan = retv.AsSpan(0, AesGcm.NonceByteSizes.MaxSize);
            var cspan = retv.AsSpan(AesGcm.NonceByteSizes.MaxSize, msg.Length);
            var tspan = retv.AsSpan(AesGcm.NonceByteSizes.MaxSize + msg.Length, AesGcm.TagByteSizes.MaxSize);

            var rng = new Random();
            rng.NextBytes(nspan);

            gcm.Encrypt(nspan, msg, cspan, tspan);
            return retv;
        }

        public byte[] Decrypt(byte[] cipher)
        {
            var nspan = cipher.AsSpan(0, AesGcm.NonceByteSizes.MaxSize);
            var cspan = cipher.AsSpan(AesGcm.NonceByteSizes.MaxSize, cipher.Length - AesGcm.TagByteSizes.MaxSize - AesGcm.NonceByteSizes.MaxSize);
            var tspan = cipher.AsSpan(cspan.Length + AesGcm.NonceByteSizes.MaxSize, AesGcm.TagByteSizes.MaxSize);

            byte[] retv = new byte[cspan.Length];
            gcm.Decrypt(nspan, cspan, tspan, retv);
            return retv;
        }
    }
}
