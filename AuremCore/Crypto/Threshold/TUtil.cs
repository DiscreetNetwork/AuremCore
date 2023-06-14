using AuremCore.Crypto.BN256;
using AuremCore.Crypto.BN256.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Crypto.Threshold
{
    internal static class TUtil
    {
        internal static BigInteger Lagrange(long[] points, long x)
        {
            var num = new BigInteger(1);
            var den = new BigInteger(1);

            foreach (var p in points)
            {
                if (p == x) continue;

                num *= new BigInteger(0 - p - 1);
                den *= new BigInteger(x - p);
            }

            den = BigInteger.ModPow(den, BN256.Constants.Order - 2, BN256.Constants.Order);

            num *= den;
            num %= BN256.Constants.Order;
            while (num < 0) num += BN256.Constants.Order;

            return num;
        }

        internal static BigInteger Poly(BigInteger[] coeffs, BigInteger x)
        {
            return coeffs.Aggregate(new BigInteger(0), (ans, y) => (ans * x + y) % BN256.Constants.Order);
        }

        public static ushort MinimalTrusted(ushort nproc)
        {
            return (ushort)((nproc - 1) /3 + 1);
        }

        public static ushort MinimalQuorum(ushort nproc)
        {
            return (ushort)(nproc - nproc / 3);
        }
    }
}
