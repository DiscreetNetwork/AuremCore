using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Crypto.BN256.Models
{
    [StructLayout(LayoutKind.Sequential)]
    public struct GT
    {
        [MarshalAs(UnmanagedType.Struct)]
        internal GFp12 p;

        public GT()
        {
            p = new GFp12();
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == null) return false;

            if (obj is GT b)
            {
                return p.Equals(b.p);
            }

            return false;
        }

        public static (SecretKey, GT) Random()
        {
            Scalar k = new Scalar();
            GT gt = new GT();
            Native.Native.Instance.RandomGT(ref gt, ref k);
            return (new SecretKey(k), gt);
        }

        public GT ScalarBaseMult(SecretKey k)
        {
            Native.Native.Instance.ScalarBaseMultGT(ref this, ref k.scalar);
            return this;
        }

        public GT ScalarBaseMult(BigInteger k)
        {
            return ScalarBaseMult(new SecretKey(k));
        }

        public GT ScalarMult(GT a, SecretKey k)
        {
            Native.Native.Instance.ScalarMultGT(ref this, ref a, ref k.scalar);
            return this;
        }

        public GT ScalarMult(GT a, BigInteger k)
        {
            return ScalarMult(a, new SecretKey(k));
        }

        public GT Add(GT a, GT b)
        {
            Native.Native.Instance.AddGT(ref this, ref a, ref b);
            return this;
        }

        public GT Neg(GT a)
        {
            Native.Native.Instance.NegGT(ref this, ref a);
            return this;
        }

        public GT Set(GT a)
        {
            p.Set(a.p);
            return this;
        }

        public byte[] Marshal()
        {
            GTEnc gtEnc = new GTEnc();
            Native.Native.Instance.MarshalGT(ref gtEnc, ref this);
            return gtEnc.bytes;
        }

        public GT Unmarshal(byte[] data)
        {
            GTEnc gtEnc = new GTEnc { bytes = data };
            Native.Native.Instance.UnmarshalGT(ref this, ref gtEnc);
            return this;
        }

        public GT Pair(G1 a, G2 b)
        {
            Native.Native.Instance.Pair(ref this, ref a, ref b);
            return this;
        }

        public GT Miller(G1 a, G2 b)
        {
            Native.Native.Instance.Miller(ref this, ref a, ref b);
            return this;
        }

        public GT Finalize()
        {
            Native.Native.Instance.FinalizeGT(ref this);
            return this;
        }

        public static GT operator +(GT a, GT b) => new GT().Add(a, b);
        public static GT operator -(GT a) => new GT().Neg(a);
        public static GT operator *(GT a, SecretKey b) => new GT().ScalarMult(a, b);
        public static GT operator *(SecretKey a, GT b) => new GT().ScalarMult(b, a);

        public static bool operator ==(GT a, GT b) => a.Equals(b);
        public static bool operator !=(GT a, GT b) => !a.Equals(b);
    }
}
