using BN256Core.Models;
using BN256Core.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace BN256Core
{
    public class SecretKey
    {
        internal Scalar scalar;
        private BigInteger s;

        private static readonly RNGCryptoServiceProvider csp = new();

        public static BigInteger RandomScalar()
        {
            byte[] data = new byte[65];
            csp.GetBytes(data, 0, 64);
            data[64] = 0;

            BigInteger b = new BigInteger(data);
            var c = b % Constants.Order;

            Array.Clear(data);
            return c;
        }

        public static BigInteger RandomScalar(int seed)
        {
            byte[] data = new byte[65];
            //csp.GetBytes(data, 0, 64);
            Random rng = new Random(seed);
            rng.NextBytes(data);
            data[64] = 0;

            BigInteger b = new BigInteger(data);
            //Console.WriteLine(b.ToString());
            var c = b % Constants.Order;

            Array.Clear(data);
            return c;
        }

        public static BigInteger RandomScalar(Random rnd)
        {
            byte[] data = new byte[65];
            rnd.NextBytes(data);
            data[64] = 0;

            BigInteger b = new BigInteger(data);
            //Console.WriteLine(b.ToString());
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

            if (c >= Constants.Order) c %= Constants.Order;
            while (c < 0) c += Constants.Order;
            s = c;

            BN.ToBN(c, scalar.n.array);
        }

        public static SecretKey operator +(SecretKey a, SecretKey b)
        {
            return new SecretKey((a.s + b.s) % Constants.Order);
        }

        public static SecretKey operator -(SecretKey a, SecretKey b)
        {
            var d = (a.s - b.s);
            while (d < 0) d += Constants.Order;
            d %= Constants.Order;
            return new SecretKey(d);
        }

        public static SecretKey operator *(SecretKey a, SecretKey b)
        {
            return new SecretKey(a.s * b.s % Constants.Order);
        }


        public void Add(BigInteger a)
        {
            s = BigInteger.Add(s, a) % Constants.Order;
            BN.ToBN(s, scalar.n.array);
        }

        public void Add(SecretKey a) => Add(a.s);

        public SecretKey Add(SecretKey a, SecretKey b)
        {
            return a + b;
        }

        public void Sub(BigInteger a)
        {
            s = ((s - a) % Constants.Order);
            while (s < 0) s += Constants.Order;
            BN.ToBN(s, scalar.n.array);
        }

        public void Sub(SecretKey a) => Sub(a.s);

        public void Mul(BigInteger a)
        {
            s = BigInteger.Multiply(s, a) % Constants.Order;
            BN.ToBN(s, scalar.n.array);
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

        public VerificationKey VerificationKey()
        {
            return new VerificationKey(new G2().ScalarBaseMult(this));
        }

        public string Encode() => Convert.ToBase64String(Marshal());

        public static SecretKey DecodeSecretKey(string enc)
        {
            if (enc == null) throw new ArgumentNullException(nameof(enc));

            var data = Convert.FromBase64String(enc);
            return new SecretKey().Unmarshal(data);
        }

        public Signature Sign(byte[] msg)
        {
            return new Signature { Sig = new G1().ScalarMult(G1.Hash(msg), this) };
        }

        public byte[] Marshal() => s.ToByteArray();

        public SecretKey Unmarshal(byte[] data)
        {
            s = new BigInteger(data);
            scalar = new();
            BN.ToBN(s, scalar.n.array);
            return this;
        }
    }
}
