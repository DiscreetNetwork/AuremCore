using AuremCore.Crypto.BN256;
using AuremCore.Crypto.Encrypt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Crypto.Threshold
{
    public class TSS
    {
        internal ushort threshold;
        internal VerificationKey globalVK;
        internal VerificationKey[] vks;
        internal SecretKey[] sks;

        public static TSS Create(ushort nproc, BigInteger[] coeffs)
        {
            ushort threshold = (ushort)coeffs.Length;
            var secret = new SecretKey(coeffs[threshold - 1]);

            var globalVK = secret.VerificationKey();

            var sks = new SecretKey[nproc];
            var vks = new VerificationKey[nproc];

            Enumerable.Range(0, nproc).Select(x => (ushort)x).AsParallel().ForAll(x =>
            {
                var secret2 = TUtil.Poly(coeffs, new BigInteger(x + 1));
                sks[x] = new SecretKey(secret2);
                vks[x] = new VerificationKey(secret2);
            });

            return new TSS { threshold = threshold, globalVK = globalVK, sks = sks, vks = vks };
        }

        public static TSS CreateRandom(ushort nproc, ushort threshold)
        {
            var coeffs = new BigInteger[threshold];
            for (int i = 0; i < threshold; i++)
            {
                coeffs[i] = SecretKey.RandomScalar();
            }

            return Create(nproc, coeffs);
        }

        public static TSS CreateSeeded(ushort nproc, ushort threshold, int seed, out BigInteger tk_0)
        {
            var coeffs = new BigInteger[threshold];
            for (int i = 0; i < threshold; i++)
            {
                coeffs[i] = SecretKey.RandomScalar(seed + i);
            }
            tk_0 = (threshold == 0) ? BigInteger.Zero : coeffs[threshold - 1];
            return Create(nproc, coeffs);
        }

        public ThresholdKey Encrypt(SymmetricKey[] encryptionKeys)
        {
            var nproc = (ushort)encryptionKeys.Length;
            var encsks = new byte[nproc][];

            for (ushort i = 0; i < nproc; i++)
            {
                encsks[i] = encryptionKeys[i].Encrypt(sks[i].Marshal());
            }

            return new ThresholdKey { threshold = threshold, globalVK = globalVK, vks = vks, encSKs = encsks };
        }
    }
}
