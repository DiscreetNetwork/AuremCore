using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Crypto.BN256.Models
{
    [StructLayout(LayoutKind.Sequential)]
    public struct G2
    {
        [MarshalAs(UnmanagedType.Struct)]
        internal TwistPoint p;

        public G2()
        {
            p = new TwistPoint();
        }

        public static (SecretKey, G2) Random()
        {
            Scalar k = new Scalar();
            G2 g2 = new G2();
            Native.Native.Instance.RandomG2(ref g2, ref k);
            return (new SecretKey(k), g2);
        }

        public G2 ScalarBaseMult(SecretKey k)
        {
            Native.Native.Instance.ScalarBaseMultG2(ref this, ref k.scalar);
            return this;
        }

        public G2 ScalarMult(G2 a, SecretKey k)
        {
            Native.Native.Instance.ScalarMultG2(ref this, ref a, ref k.scalar);
            return this;
        }

        public G2 Add(G2 a, G2 b)
        {
            Native.Native.Instance.AddG2(ref this, ref a, ref b);
            return this;
        }

        public G2 Neg(G2 a)
        {
            Native.Native.Instance.NegG2(ref this, ref a);
            return this;
        }

        public G2 Set(G2 a)
        {
            p.Set(a.p);
            return this;
        }

        public byte[] Marshal()
        {
            G2Enc g2Enc = new G2Enc();
            Native.Native.Instance.MarshalG2(ref g2Enc, ref this);
            return g2Enc.bytes;
        }

        public G2 Unmarshal(byte[] data)
        {
            G2Enc g2Enc = new G2Enc { bytes = data };
            Native.Native.Instance.UnmarshalG2(ref this, ref g2Enc);
            return this;
        }

        public static G2 operator +(G2 a, G2 b) => new G2().Add(a, b);
        public static G2 operator -(G2 a) => new G2().Neg(a);
        public static G2 operator *(G2 a, SecretKey b) => new G2().ScalarMult(a, b);
        public static G2 operator *(SecretKey a, G2 b) => new G2().ScalarMult(b, a);

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is G2 b)
            {
                return p.Equals(b.p);
            }

            return false;
        }

        public static bool operator ==(G2 a, G2 b) => a.Equals(b);
        public static bool operator !=(G2 a, G2 b) => !a.Equals(b);

        public static GT operator *(G1 a, G2 b) => new GT().Pair(a, b);
        public static GT operator *(G2 a, G1 b) => new GT().Pair(b, a);
    }
}
