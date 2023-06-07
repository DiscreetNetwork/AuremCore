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
            G1 g1 = new G1();
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
            Console.WriteLine(ng1.p.ToString());
        }
    }
}