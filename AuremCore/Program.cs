using System;
using System.Text;
using AuremCore.Crypto.BN256.Common;
using AuremCore.Crypto.BN256.Models;
using AuremCore.Crypto.BN256.Native;

namespace AuremCore
{
    public class Program
    {
        public static void TestMiller()
        {
            G2 g2 = new G2();
            Scalar k2 = new Scalar();
            k2.n.array[0] = 292929292;
            //G2Enc g2Enc = new G2Enc();
            //G2 h = new G2();
            Native.Instance.ScalarBaseMultG2(ref g2, ref k2);

            G1 g1 = new G1();
            Scalar k1 = new Scalar();
            k1.n.array[0] = 30291;

            Native.Instance.ScalarBaseMultG1(ref g1, ref k1);

            GT gt = new GT();
            Native.Instance.Miller(ref gt, ref g1, ref g2);
            Native.Instance.FinalizeGT(ref gt);
            GTEnc enc = new GTEnc();
            //Native.Instance.MarshalGT(ref enc, ref gt);
            //Console.WriteLine(PrintUtil.Hexify(enc.bytes));
            Console.WriteLine(gt.p.ToString());

            Scalar kt = new Scalar();
            kt.n.array[0] = 0xef3c78e4;
            kt.n.array[1] = 0x811;
            GT testGood = new GT();
            Native.Instance.ScalarBaseMultGT(ref testGood, ref kt);
            Console.WriteLine(testGood.p.ToString());

        }

        public static void TestLineFuncAdd()
        {
            GFp2 b2 = new GFp2();
            Native.Instance.NewGFp(b2.x, -123);
            Native.Instance.NewGFp(b2.y, 456);

            G2 g2 = new G2();
            Scalar k2 = new Scalar();
            k2.n.array[0] = 789;
            //G2Enc g2Enc = new G2Enc();
            //G2 h = new G2();
            Native.Instance.ScalarBaseMultG2(ref g2, ref k2);


            G2 q2 = new G2();
            Scalar qk2 = new Scalar();
            qk2.n.array[0] = 876;
            //G2Enc g2Enc = new G2Enc();
            //G2 h = new G2();
            Native.Instance.ScalarBaseMultG2(ref q2, ref qk2);

            G1 g1 = new G1();
            Scalar k1 = new Scalar();
            k1.n.array[0] = 543;

            Native.Instance.ScalarBaseMultG1(ref g1, ref k1);

            //Console.WriteLine("G2: " + g2.p.ToString());
            //Console.WriteLine("Q2: " + q2.p.ToString());
            //Console.WriteLine("G1: " + g1.p.ToString());

            GFp2 a = new GFp2();
            GFp2 b = new GFp2();
            GFp2 c = new GFp2();
            TwistPoint rout = new TwistPoint();

            Native.Instance.LineFuncDouble(ref a, ref b, ref c, ref rout, ref g2.p, ref g1.p);

            G2 rout2 = new G2 { p = rout };

            G2Enc g2e = new G2Enc();
            G2Enc q2e = new G2Enc();
            G1Enc g1e = new G1Enc();
            G2Enc route = new G2Enc();
            //Native.Instance.MarshalG2(ref g2e, ref g2);
            //Native.Instance.MarshalG2(ref q2e, ref q2);
            //Native.Instance.MarshalG1(ref g1e, ref g1);
            Native.Instance.MarshalG2(ref route, ref rout2);
            

            //Console.WriteLine("G2: " + g2e.ToString());
            //Console.WriteLine("G1: " + g1e.ToString());
            //Console.WriteLine("Q2: " + q2e.ToString());
            //Console.WriteLine("B2: " + b2.ToString());
            Console.WriteLine("A: " + a.ToString());
            Console.WriteLine("B: " + b.ToString());
            Console.WriteLine("C: " + c.ToString());
            Console.WriteLine("OUT: " + rout.ToString());
            Console.WriteLine("OUT!: " + route.ToString());

            //Console.WriteLine("\n\nsanity check:\none?? = " + g2.p.ToString());

            //GT gt = new GT();
            //Native.Instance.Miller(ref gt, ref g1, ref g2);
            //GTEnc enc = new GTEnc();
            //Native.Instance.MarshalGT(ref enc, ref gt);
            //Console.WriteLine(PrintUtil.Hexify(enc.bytes));
        }

        public static void Main(string[] args)
        {
            // testing code
            /*G1 g1 = new G1();
            G1Enc g1Enc = new G1Enc();
            Scalar s = new Scalar();
            s.n.array[0] = 2;
            Console.WriteLine(Native.Instance.ScalarBitlen(ref s));
            Console.WriteLine(Native.Instance.ScalarBit(ref s, 2));
            Console.WriteLine(Native.Instance.ScalarBit(ref s, 1));
            Console.WriteLine(Native.Instance.ScalarBit(ref s, 0));


            ulong[] gfpa = new ulong[4];
            ulong[] gfpb = new ulong[4];
            ulong[] gfpc = new ulong[4];

            Native.Instance.NewGFp(gfpa, -94381238189);
            Native.Instance.NewGFp(gfpb, 1);
            Native.Instance.GFpMul(gfpc, gfpa, gfpb);

            ulong[] gfpd = new ulong[4] { 94381238189, 198273017, 73738398, 10192928 };
            ulong[] gfpe = new ulong[4];
            Native.Instance.GFpNeg(gfpe, gfpd);

            var ebytes = Util.FpToBytes(gfpe);
            Console.WriteLine($"E bytes: {PrintUtil.Hexify(ebytes)}");

            var abytes = Util.FpToBytes(gfpa);
            Console.WriteLine($"A bytes: {PrintUtil.Hexify(abytes)}");
            var bbytes = Util.FpToBytes(gfpb);
            Console.WriteLine($"B bytes: {PrintUtil.Hexify(bbytes)}");
            var cbytes = Util.FpToBytes(gfpc);
            Console.WriteLine($"XYZW yeah: {PrintUtil.Hexify(cbytes)}");


            Native.Instance.ScalarBaseMultG1(ref g1, ref s);
            Native.Instance.MarshalG1(ref g1Enc, ref g1);
            var str = PrintUtil.Hexify(g1Enc.bytes);
            Console.WriteLine(str);
            Console.WriteLine(g1.p.ToString());
            //Native.Instance.RandomG1.Invoke(ref g1, ref s);
            int a = 0;
            int b = 2;
            Native.Instance.TestIfWorks.Invoke(ref a, b);
            Console.WriteLine(a);
            G1 ng1 = new G1();
            G1 zg1 = new G1();
            Native.Instance.NegG1(ref ng1, ref zg1);
            Console.WriteLine(ng1.p.ToString());*/

            /*ulong[] a = new ulong[4];
            ulong[] b = new ulong[4];
            ulong[] c = new ulong[4] { 318734, 87387932842, 283719237, 1283878282 };
            ulong[] d = new ulong[4];
            ulong[] e = new ulong[4];
            ulong[] f = new ulong[4];

            Native.Instance.NewGFp(a, -94381238189);
            Native.Instance.GFpInvert(b, a);
            Native.Instance.GFpSqrt(d, c);

            var abytes = Util.FpToBytes(a);
            Console.WriteLine($"A bytes: {PrintUtil.Hexify(abytes)}");
            var bbytes = Util.FpToBytes(b);
            Console.WriteLine($"B bytes: {PrintUtil.Hexify(bbytes)}");
            var cbytes = Util.FpToBytes(c);
            Console.WriteLine($"C bytes: {PrintUtil.Hexify(cbytes)}");
            var dbytes = Util.FpToBytes(d);
            Console.WriteLine($"D bytes: {PrintUtil.Hexify(dbytes)}");

            G1 g = new G1();
            G1Enc gEnc = new G1Enc();
            Scalar k = new Scalar();
            k.n.array[0] = 2;
            Native.Instance.ScalarBaseMultG1(ref g, ref k);
            Native.Instance.MarshalG1(ref gEnc, ref g);

            Native.Instance.NewGFp(a, -2);
            Native.Instance.NewGFp(b, 1);
            Native.Instance.NewGFp(e, 3);
            Native.Instance.GFpMul(c, a, a);
            Native.Instance.GFpMul(d, b, b);
            Native.Instance.GFpMul(f, d, b);
            Native.Instance.GFpAdd(d, f, e);

            abytes = Util.FpToBytes(a);
            Console.WriteLine($"G_y bytes: {PrintUtil.Hexify(abytes)}");
            bbytes = Util.FpToBytes(b);
            Console.WriteLine($"G_x bytes: {PrintUtil.Hexify(bbytes)}");
            cbytes = Util.FpToBytes(c);
            Console.WriteLine($"y^2 bytes: {PrintUtil.Hexify(cbytes)}");
            dbytes = Util.FpToBytes(d);
            Console.WriteLine($"x^2 bytes: {PrintUtil.Hexify(dbytes)}");

            Console.WriteLine($"E bytes: {PrintUtil.Hexify(gEnc.bytes)}");

            Console.WriteLine($"{{\n\t{PrintUtil.Hexify(bbytes)},\n\t{PrintUtil.Hexify(abytes)},\n\t{PrintUtil.Hexify(bbytes)},\n\t{PrintUtil.Hexify(bbytes)}\n}}");

            ulong[] cb = new ulong[4];
            Native.Instance.NewGFp(cb, 3);
            var cbbytes = Util.FpToBytes(cb);
            Console.WriteLine(PrintUtil.Hexify(cbbytes));

            ulong[] z = new ulong[4];
            ulong[] o = new ulong[4];
            Native.Instance.NewGFp(z, 0);
            Native.Instance.NewGFp(o, 1);
            var zbytes = Util.FpToBytes(z);
            var obytes = Util.FpToBytes(o);
            Console.WriteLine($"\t{{\n\t\t{PrintUtil.Hexify(zbytes)},\n\t\t{PrintUtil.Hexify(obytes)}\n\t}},\n\t{{\n\t\t  {PrintUtil.Hexify(zbytes)},\n\t\t{PrintUtil.Hexify(obytes)}\n\t}}");
            */
            /*G1 g = new G1();
            G1Enc gEnc = new G1Enc();
            Scalar k = new Scalar();
            k.n.array[0] = 1234567890;
            Native.Instance.ScalarBaseMultG1(ref g, ref k);
            Native.Instance.MarshalG1(ref gEnc, ref g);
            Console.WriteLine(PrintUtil.Hexify(gEnc.bytes));
            Native.Instance.HashG1(ref g, Encoding.ASCII.GetBytes("discreet"), (ulong)Encoding.ASCII.GetBytes("discreet").Length, new byte[0], 0);
            Native.Instance.MarshalG1(ref gEnc, ref g);
            Console.WriteLine(PrintUtil.Hexify(gEnc.bytes));
            Console.WriteLine(g.p.ToString());
            G1 gX = new G1();
            Scalar x = new Scalar();
            x.n.array[0] = 123456789;

            G1 gbase = new G1 { 
                p = new CurvePoint {
                    x = new ulong[4] { 0xaa298958ef632957, 0x2a7c4bea0099349e, 0x92e4852421fa5b77, 0xfe4daac9663cd805 },
                    y = new ulong[4] { 0x4341a3d715860914, 0x259110952ad07fa1, 0xf2852dd76a0f108c, 0xc81cfc9a62383c1b },
                    z = new ulong[4] { 0x9969f7a19353a3e7, 0x614a4adf2e77a411, 0xde237b9e47139055, 0x06785cb51cfe4a70 },
                    t = new ulong[4] { 0x9969f7a19353a3e7, 0x614a4adf2e77a411, 0xde237b9e47139055, 0x06785cb51cfe4a70 },
                }
            };

             

            Native.Instance.ScalarMultG1(ref gX, ref gbase, ref x);
            Native.Instance.MarshalG1(ref gEnc, ref gX);
            Console.WriteLine(PrintUtil.Hexify(gEnc.bytes));

            G1 gX_actual = new G1();
            Native.Instance.ScalarMultG1(ref gX_actual, ref g, ref x);
            Native.Instance.MarshalG1(ref gEnc, ref gX_actual);
            Console.WriteLine(PrintUtil.Hexify(gEnc.bytes));*/



            //Native.Instance.AddG2(ref h, ref g, ref g);
            //Native.Instance.MarshalG2(ref g2Enc, ref h);
            //Console.WriteLine(h.p.ToString());
            //Native.Instance.MarshalG2(ref g2Enc, ref g);
            //Console.WriteLine(g2Enc.ToString());
            //ulong[] z = new ulong[4];
            //ulong[] o = new ulong[4];
            //Native.Instance.NewGFp(z, 0);
            //Native.Instance.NewGFp(o, 1);
            //Console.WriteLine(PrintUtil.Hexify(Util.FpToBytes(z), true));
            //Console.WriteLine(PrintUtil.Hexify(Util.FpToBytes(o), true));
            //Console.WriteLine("\n\n");
            //Console.WriteLine(g.p.ToString());

            /*GFp2 a = new GFp2();
            GFp2 b = new GFp2();
            GFp2 c = new GFp2();

            Native.Instance.NewGFp(a.x, 59230818);
            Native.Instance.NewGFp(a.y, 12930170972);
            Native.Instance.NewGFp(b.x, 871290387612);
            Native.Instance.NewGFp(b.y, -1237373738389);


            Native.Instance.GFp2Invert(ref c, ref b);
            Console.WriteLine(PrintUtil.Hexify(Util.FpToBytes(c.x), true) + ", " + PrintUtil.Hexify(Util.FpToBytes(c.y), true));*/

            GFp6 a6 = new GFp6();
            Native.Instance.NewGFp(a6.x.x, 59230818);
            Native.Instance.NewGFp(a6.x.y, 12930170972);
            Native.Instance.NewGFp(a6.y.x, 871290387612);
            Native.Instance.NewGFp(a6.y.y, -1237373738389);
            Native.Instance.NewGFp(a6.z.x, -8393723729800);
            Native.Instance.NewGFp(a6.z.y, -3920189001900);

            GFp6 b6 = new GFp6();
            Native.Instance.NewGFp(b6.x.x, -1289031872389);
            Native.Instance.NewGFp(b6.x.y, 99928298292);
            Native.Instance.NewGFp(b6.y.x, 3562828282);
            Native.Instance.NewGFp(b6.y.y, -33030303031999);
            Native.Instance.NewGFp(b6.z.x, -2086740192333);
            Native.Instance.NewGFp(b6.z.y, -5555555555552);

            GFp6 c6 = new GFp6();
            Native.Instance.NewGFp(c6.x.x, -81738912673);
            Native.Instance.NewGFp(c6.x.y, -19182828282);
            Native.Instance.NewGFp(c6.y.x, 787878780000);
            Native.Instance.NewGFp(c6.y.y, 302189382);
            Native.Instance.NewGFp(c6.z.x, 914914914914);
            Native.Instance.NewGFp(c6.z.y, -392001900);

            GFp6 d6 = new GFp6();
            Native.Instance.NewGFp(d6.x.x, 428719891);
            Native.Instance.NewGFp(d6.x.y, 684567282);
            Native.Instance.NewGFp(d6.y.x, -3717891291);
            Native.Instance.NewGFp(d6.y.y, -9654324535);
            Native.Instance.NewGFp(d6.z.x, 1231231231234);
            Native.Instance.NewGFp(d6.z.y, 7534233454323);

            GFp12 a12 = new GFp12 { x = a6, y = b6 };
            GFp12 b12 = new GFp12 { x = c6, y = d6 };

            GFp2 a2 = new GFp2();
            Native.Instance.NewGFp(a2.x, 1231287);
            Native.Instance.NewGFp(a2.y, -129339);

            GFp2 b2 = new GFp2();
            Native.Instance.NewGFp(b2.x, -123);
            Native.Instance.NewGFp(b2.y, 456);

            GFp2 c2 = new GFp2();
            Native.Instance.NewGFp(c2.x, -3903939);
            Native.Instance.NewGFp(c2.y, -39393939);

            ulong[] b1 = new ulong[4];
            Native.Instance.NewGFp(b1, -999);

            GFp12 c12 = new GFp12();

            Scalar k = new Scalar();
            k.n.array[0] = 1234567890;

            Native.Instance.GFp12Invert(ref c12, ref a12);

            //Console.WriteLine(c12.ToString());

            //GFp2 c2 = new GFp2();
            //Native.Instance.GFp2MulXi(ref c2, ref b2);
            //Console.WriteLine(c2.ToString());

            //GFp6 c6 = new GFp6();
            //Native.Instance.GFp6Square(ref c6, ref a6);
            //Console.WriteLine(c6.ToString());

            //Console.WriteLine("\n\n\n");
            //TestMiller();

            TestMiller();

            //TestLineFuncAdd();

            //Scalar k = new Scalar();
            //Console.WriteLine(Native.Instance.ScalarBitlen(ref k));

            //G2 z = new G2();
            //G2 z2 = new G2();
            //Native.Instance.AddG2(ref z2, ref z, ref z);
            //Native.Instance.GFp2Mul(ref z2, ref z, ref z);
            //Console.WriteLine(z2.p.ToString());
        }
    }
}