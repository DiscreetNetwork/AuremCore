using System;
using System.Text;
using AuremCore.BN256.Common;
using AuremCore.BN256.Models;
using AuremCore.BN256.Native;

namespace AuremCore
{
    public class Program
    {
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

            G2 g = new G2();
            Scalar k2 = new Scalar();
            k2.n.array[0] = 292929292;
            G2Enc g2Enc = new G2Enc();
            //G2 h = new G2();
            Native.Instance.ScalarBaseMultG2(ref g, ref  k2);
            //Native.Instance.AddG2(ref h, ref g, ref g);
            //Native.Instance.MarshalG2(ref g2Enc, ref h);
            //Console.WriteLine(h.p.ToString());
            Native.Instance.MarshalG2(ref g2Enc, ref g);
            Console.WriteLine(g2Enc.ToString());
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
        }
    }
}