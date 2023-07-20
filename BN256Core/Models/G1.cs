using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BN256Core.Models
{
    [StructLayout(LayoutKind.Sequential)]
    public struct G1
    {
        [MarshalAs(UnmanagedType.Struct)]
        public CurvePoint p;

        public G1()
        {
            p = new CurvePoint();
        }

        public static G1 Hash(ReadOnlySpan<byte> msg)
        {
            G1 result = new G1();
            Native.Native.Instance.HashG1(ref result, msg.ToArray(), (ulong)msg.Length, Constants.DST, (ulong)Constants.DST.Length);
            return result;
        }

        public static (SecretKey, G1) Random()
        {
            var sk = new Scalar();
            var g1 = new G1();
            Native.Native.Instance.RandomG1(ref g1, ref sk);
            return (new SecretKey(sk), g1);
        }

        public G1 ScalarBaseMult(SecretKey k)
        {
            Native.Native.Instance.ScalarBaseMultG1(ref this, ref k.scalar);
            return this;
        }

        public G1 ScalarBaseMult(BigInteger k)
        {
            return ScalarBaseMult(new SecretKey(k));
        }

        public G1 ScalarMult(G1 a, SecretKey k)
        {
            Native.Native.Instance.ScalarMultG1(ref this, ref a, ref k.scalar);
            return this;
        }

        public G1 ScalarMult(G1 a, BigInteger k)
        {
            return ScalarMult(a, new SecretKey(k));
        }

        public G1 Add(G1 a, G1 b)
        {
            Native.Native.Instance.AddG1(ref this, ref a, ref b);
            return this;
        }

        public G1 Neg(G1 a)
        {
            Native.Native.Instance.NegG1(ref this, ref a);
            return this;
        }

        public G1 Set(G1 a)
        {
            p.Set(a.p);
            return this;
        }

        public byte[] Marshal()
        {
            G1Enc g1Enc = new G1Enc();
            Native.Native.Instance.MarshalG1(ref g1Enc, ref this);
            return g1Enc.bytes;
        }

        public G1 Unmarshal(ReadOnlySpan<byte> dat)
        {
            G1Enc g1Enc = new G1Enc { bytes = dat.ToArray() };
            Native.Native.Instance.UnmarshalG1(ref this, ref g1Enc);
            return this;
        }

        public static G1 operator +(G1 a, G1 b) => new G1().Add(a, b);
        public static G1 operator -(G1 a) => new G1().Neg(a);
        public static G1 operator *(G1 a, SecretKey b) => new G1().ScalarMult(a, b);
        public static G1 operator *(SecretKey a, G1 b) => new G1().ScalarMult(b, a);

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is G1 b)
            {
                return p.Equals(b.p);
            }

            return false;
        }

        public static bool operator ==(G1 a, G1 b) => a.Equals(b);
        public static bool operator !=(G1 a, G1 b) => !a.Equals(b);
    }
}
