using AuremCore.Crypto.BN256.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Crypto.BN256.Native
{
    internal static class bn256
    {
        [DllImport("bn256")]
        public static extern void TestIfWorks(ref int a, int b);

        [DllImport("bn256")]
        public static extern ulong CheckBMI2();

        [DllImport("bn256")]
        public static extern int ScalarBitlen(ref Scalar k);

        [DllImport("bn256")]
        public static extern int ScalarBit(ref Scalar k, int i);

        [DllImport("bn256")]
        public static extern void GFpMul([In, Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U8, SizeConst = 4)] ulong[] c,
            [In, Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U8, SizeConst = 4)] ulong[] a,
            [In, Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U8, SizeConst = 4)] ulong[] b);

        [DllImport("bn256")]
        public static extern void NewGFp([In, Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U8, SizeConst = 4)] ulong[] a, long x);

        [DllImport("bn256")]
        public static extern void GFpNeg([In, Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U8, SizeConst = 4)] ulong[] c,
            [In, Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U8, SizeConst = 4)] ulong[] a);

        [DllImport("bn256")]
        public static extern void GFpExp([In, Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U8, SizeConst = 4)] ulong[] c,
            [In, Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U8, SizeConst = 4)] ulong[] a,
            [In, Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U8, SizeConst = 4)] ulong[] bits);

        [DllImport("bn256")]
        public static extern void GFpInvert([In, Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U8, SizeConst = 4)] ulong[] c,
            [In, Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U8, SizeConst = 4)] ulong[] a);

        [DllImport("bn256")]
        public static extern void GFpSqrt([In, Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U8, SizeConst = 4)] ulong[] c,
            [In, Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U8, SizeConst = 4)] ulong[] a);

        [DllImport("bn256")]
        public static extern void GFpAdd([In, Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U8, SizeConst = 4)] ulong[] c,
            [In, Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U8, SizeConst = 4)] ulong[] a,
            [In, Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U8, SizeConst = 4)] ulong[] b);

        [DllImport("bn256")]
        public static extern void GFpSub([In, Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U8, SizeConst = 4)] ulong[] c,
            [In, Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U8, SizeConst = 4)] ulong[] a,
            [In, Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U8, SizeConst = 4)] ulong[] b);

        [DllImport("bn256")]
        public static extern void GFp2Add(ref GFp2 c, ref GFp2 a, ref GFp2 b);

        [DllImport("bn256")]
        public static extern void GFp2Sub(ref GFp2 c, ref GFp2 a, ref GFp2 b);

        [DllImport("bn256")]
        public static extern void GFp2Mul(ref GFp2 c, ref GFp2 a, ref GFp2 b);

        [DllImport("bn256")]
        public static extern void GFp2Square(ref GFp2 c, ref GFp2 a);

        [DllImport("bn256")]
        public static extern void GFp2Invert(ref GFp2 c, ref GFp2 a);

        [DllImport("bn256")]
        public static extern void GFp2MulXi(ref GFp2 c, ref GFp2 a);

        [DllImport("bn256")]
        public static extern void GFp6Frobenius(ref GFp6 c, ref GFp6 a);

        [DllImport("bn256")]
        public static extern void GFp6FrobeniusP2(ref GFp6 c, ref GFp6 a);

        [DllImport("bn256")]
        public static extern void GFp6FrobeniusP4(ref GFp6 c, ref GFp6 a);

        [DllImport("bn256")]
        public static extern void GFp6Add(ref GFp6 c, ref GFp6 a, ref GFp6 b);

        [DllImport("bn256")]
        public static extern void GFp6Sub(ref GFp6 c, ref GFp6 a, ref GFp6 b);

        [DllImport("bn256")]
        public static extern void GFp6Mul(ref GFp6 c, ref GFp6 a, ref GFp6 b);

        [DllImport("bn256")]
        public static extern void GFp6MulScalar(ref GFp6 c, ref GFp6 a, ref GFp2 b);

        [DllImport("bn256")]
        public static extern void GFp6MulGFp(ref GFp6 c, ref GFp6 a, [In, Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U8, SizeConst = 4)] ulong[] b);

        [DllImport("bn256")]
        public static extern void GFp6MulTau(ref GFp6 c, ref GFp6 a);

        [DllImport("bn256")]
        public static extern void GFp6Square(ref GFp6 c, ref GFp6 a);

        [DllImport("bn256")]
        public static extern void GFp6Invert(ref GFp6 c, ref GFp6 a);

        [DllImport("bn256")]
        public static extern void GFp12Conjugate(ref GFp12 c, ref GFp12 a);

        [DllImport("bn256")]
        public static extern void GFp12Neg(ref GFp12 c, ref GFp12 a);

        [DllImport("bn256")]
        public static extern void GFp12Frobenius(ref GFp12 c, ref GFp12 a);

        [DllImport("bn256")]
        public static extern void GFp12FrobeniusP2(ref GFp12 c, ref GFp12 a);

        [DllImport("bn256")]
        public static extern void GFp12FrobeniusP4(ref GFp12 c, ref GFp12 a);

        [DllImport("bn256")]
        public static extern void GFp12Add(ref GFp12 c, ref GFp12 a, ref GFp12 b);

        [DllImport("bn256")]
        public static extern void GFp12Sub(ref GFp12 c, ref GFp12 a, ref GFp12 b);

        [DllImport("bn256")]
        public static extern void GFp12Mul(ref GFp12 c, ref GFp12 a, ref GFp12 b);

        [DllImport("bn256")]
        public static extern void GFp12MulScalar(ref GFp12 c, ref GFp12 a, ref GFp6 b);

        [DllImport("bn256")]
        public static extern void GFp12Square(ref GFp12 c, ref GFp12 a);

        [DllImport("bn256")]
        public static extern void GFp12Invert(ref GFp12 c, ref GFp12 a);

        [DllImport("bn256")]
        public static extern void GFp12Exp(ref GFp12 c, ref GFp12 a, ref Scalar power);

        [DllImport("bn256")]
        public static extern void LineFuncAdd(ref GFp2 a, ref GFp2 b, ref GFp2 c, ref TwistPoint rout, ref TwistPoint r, ref TwistPoint p, ref CurvePoint q, ref GFp2 rr2);

        [DllImport("bn256")]
        public static extern void LineFuncDouble(ref GFp2 a, ref GFp2 b, ref GFp2 c, ref TwistPoint rout, ref TwistPoint r, ref CurvePoint q);

        [DllImport("bn256")]
        public static extern void MulLine(ref GFp12 ret, ref GFp2 a, ref GFp2 b, ref GFp2 c);

        [DllImport("bn256")]
        public static extern void RandomG1(ref G1 g1, ref Scalar k);

        [DllImport("bn256")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern G1 RandomG1_1();

        [DllImport("bn256")]
        public static extern void ScalarBaseMultG1(ref G1 gk, ref Scalar k);

        [DllImport("bn256")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern G1 ScalarBaseMultG1_1(ref Scalar k);

        [DllImport("bn256")]
        public static extern void ScalarMultG1(ref G1 ak, ref G1 a, ref Scalar k);

        [DllImport("bn256")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern G1 ScalarMultG1_1(ref G1 a, ref Scalar k);

        [DllImport("bn256")]
        public static extern void AddG1(ref G1 ab, ref G1 a, ref G1 b);

        [DllImport("bn256")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern G1 AddG1_1(ref G1 a, ref G1 b);

        [DllImport("bn256")]
        public static extern void NegG1(ref G1 na, ref G1 a);

        [DllImport("bn256")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern G1 NegG1_1(ref G1 a);

        [DllImport("bn256")]
        public static extern void MarshalG1(ref G1Enc res, ref G1 a);

        [DllImport("bn256")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern G1Enc MarshalG1_1(ref G1 a);

        [DllImport("bn256")]
        public static extern void UnmarshalG1(ref G1 a, ref G1Enc e);

        [DllImport("bn256")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern G1 UnmarshalG1_1(ref G1Enc e);


        [DllImport("bn256")]
        public static extern void RandomG2(ref G2 g2, ref Scalar k);

        [DllImport("bn256")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern G2 RandomG2_1();

        [DllImport("bn256")]
        public static extern void ScalarBaseMultG2(ref G2 gk, ref Scalar k);

        [DllImport("bn256")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern G2 ScalarBaseMultG2_1(ref Scalar k);

        [DllImport("bn256")]
        public static extern void ScalarMultG2(ref G2 ak, ref G2 a, ref Scalar k);

        [DllImport("bn256")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern G2 ScalarMultG2_1(ref G2 a, ref Scalar k);

        [DllImport("bn256")]
        public static extern void AddG2(ref G2 ab, ref G2 a, ref G2 b);

        [DllImport("bn256")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern G2 AddG2_1(ref G2 a, ref G2 b);

        [DllImport("bn256")]
        public static extern void NegG2(ref G2 na, ref G2 a);

        [DllImport("bn256")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern G2 NegG2_1(ref G2 a);

        [DllImport("bn256")]
        public static extern void MarshalG2(ref G2Enc res, ref G2 a);

        [DllImport("bn256")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern G2Enc MarshalG2_1(ref G2 a);

        [DllImport("bn256")]
        public static extern void UnmarshalG2(ref G2 a, ref G2Enc e);

        [DllImport("bn256")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern G2 UnmarshalG2_1(ref G2Enc e);


        [DllImport("bn256")]
        public static extern void RandomGT(ref GT gt, ref Scalar k);

        [DllImport("bn256")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern GT RandomGT_1();

        [DllImport("bn256")]
        public static extern void ScalarBaseMultGT(ref GT gk, ref Scalar k);

        [DllImport("bn256")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern GT ScalarBaseMultGT_1(ref Scalar k);

        [DllImport("bn256")]
        public static extern void ScalarMultGT(ref GT ak, ref GT a, ref Scalar k);

        [DllImport("bn256")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern GT ScalarMultGT_1(ref GT a, ref Scalar k);

        [DllImport("bn256")]
        public static extern void AddGT(ref GT ab, ref GT a, ref GT b);

        [DllImport("bn256")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern GT AddGT_1(ref GT a, ref GT b);

        [DllImport("bn256")]
        public static extern void NegGT(ref GT na, ref GT a);

        [DllImport("bn256")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern GT NegGT_1(ref GT a);

        [DllImport("bn256")]
        public static extern void MarshalGT(ref GTEnc res, ref GT a);

        [DllImport("bn256")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern GTEnc MarshalGT_1(ref GT a);

        [DllImport("bn256")]
        public static extern void UnmarshalGT(ref GT a, ref GTEnc e);

        [DllImport("bn256")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern GT UnmarshalGT_1(ref GTEnc e);


        [DllImport("bn256")]
        public static extern void Pair(ref GT gt, ref G1 p, ref G2 q);

        [DllImport("bn256")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern GT Pair_1(ref G1 p, ref G2 q);

        [DllImport("bn256")]
        public static extern void Miller(ref GT gt, ref G1 p, ref G2 q);

        [DllImport("bn256")]
        [return: MarshalAs(UnmanagedType.Struct)]
        public static extern GT Miller_1(ref G1 p, ref G2 q);

        [DllImport("bn256")]
        public static extern void Finalize(ref GT gt);


        [DllImport("bn256")]
        public static extern void HashG1(ref G1 res, [In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] msg, ulong msg_len, [In, Out][MarshalAs(UnmanagedType.LPArray)] byte[] dst, ulong dst_len);
    }
}
