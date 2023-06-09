using AuremCore.Crypto.BN256.Models;
using AuremCore.Crypto.BN256.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Crypto.BN256
{
    public class SecretKey
    {
        internal Scalar scalar;
        private BigInteger s;

        private static readonly RNGCryptoServiceProvider csp = new();

        private static BigInteger RandomScalar()
        {
            byte[] data = new byte[65];
            csp.GetBytes(data, 0, 64);
            data[64] = 0;

            BigInteger b = new BigInteger(data);
            var c = b % Constants.Order;

            Array.Clear(data);
            return c;
        }

        public SecretKey()
        {
            s = new BigInteger(0);
            scalar = new Scalar(s);
        }

        internal SecretKey(Scalar other)
        {
            scalar = new();
            for (int i = 0; i < scalar.n.array.Length; i++)
            {
                scalar.n.array[i] = other.n.array[i];
            }

            s = BN.FromBN(other.n);
        }

        public SecretKey(SecretKey other) : this(other.scalar) { }

        public SecretKey(BigInteger c)
        {
            scalar = new();
            s = c;
            BN.ToBN(c, scalar.n.array);
        }

        public static SecretKey operator +(SecretKey a, SecretKey b)
        {
            return new SecretKey((a.s + b.s) % Constants.Order);
        }

        public static SecretKey operator -(SecretKey a, SecretKey b)
        {
            return new SecretKey(BigInteger.Abs((a.s - b.s) % Constants.Order));
        }

        public static SecretKey operator *(SecretKey a, SecretKey b)
        {
            return new SecretKey(a.s * b.s % Constants.Order);
        }


        public void Add(BigInteger a)
        {
            s = BigInteger.Add(s, a) % Constants.Order;
            BN.ToBN(a, scalar.n.array);
        }

        public void Add(SecretKey a) => Add(a.s);

        public void Sub(BigInteger a)
        {
            s = BigInteger.Abs((s - a) % Constants.Order);
            BN.ToBN(a, scalar.n.array);
        }

        public void Sub(SecretKey a) => Sub(a.s);

        public void Mul(BigInteger a)
        {
            s = BigInteger.Multiply(s, a) % Constants.Order;
            BN.ToBN(a, scalar.n.array);
        }

        public void Mul(SecretKey a) => Mul(a.s);

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == null) return false;
            if (obj is SecretKey b)
            {
                return b.s.Equals(s);
            }

            return false;
        }
    }
}
