using CryptoLib2.Comp;
using CryptoLib2;
using System;
using System.Numerics;
using BN256Core.Models;
using BN256Core;
using System.Text;

namespace CryptoLib2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            SimpleProgram sp = SimpleProgram.Create();
            var witness = sp.Execute(new List<BigInteger> { 3 });

            R1CS r1cs = new R1CS(sp).Simplify();

            var negativeThreeAndTwoThirds = BigInteger.Parse("43333699797097735821864292494906603828380270702602524259920708646029444533309");
            var negativeEleven = negativeThreeAndTwoThirds * 3 % Constants.Order;
            //Console.WriteLine(negativeEleven.ToString());

            //Console.WriteLine(r1cs.ToString());
            //Console.WriteLine($"Witness: [{R1CSLine.BIAtoS(witness.ToArray())}]");

            QAP qap = new QAP(r1cs);
            qap.VerifyCorrectness(r1cs);
            //qap.VerifyCorrectnessWithPrint(r1cs);

            var x = new G2().ScalarBaseMult(1);
            var y = new G2();
            y.Set(x);
            //if (x.Equals(y)) throw new Exception("look elsewhere");

            //Console.WriteLine(R1CSLine.BIAtoS(qap.EvaluateAs(1)));
            //Console.WriteLine(R1CSLine.BIAtoS(qap.A[1]));

            //var fiveSixths = BigInteger.Parse("10833424949274433955466073123726650957095067675650631064980177161507361133329");
            //var ninePlusOneSixth = BigInteger.Parse("54167124746372169777330365618633254785475338378253155324900885807536805666650");
            //var oneSixth = BigInteger.ModPow(6, Constants.Order - 2, Constants.Order);
            //Console.WriteLine(oneSixth.ToString());
            //Console.WriteLine((ninePlusOneSixth - 9).ToString());
            //var one = (fiveSixths + oneSixth) % Constants.Order;
            //Console.WriteLine(one.ToString());

            /*(var h, var rem) = qap.CalculatePoly(witness);
            if (!QAP.NoRemainder(rem)) throw new Exception("Remainder of valid witness to circuit should be zero");
            //Console.WriteLine($"H: [{R1CSLine.BIAtoS(h)}]");

            Groth16CRS crs = new Groth16CRS(sp);

            var numTest = 1000;
            Groth16Proof[] testProofs = new Groth16Proof[numTest];
            var rngg = new Random();
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < numTest; i++)
            {
                var testWitness = sp.Execute(new List<BigInteger> { rngg.Next(1048576) });
                testProofs[i] = crs.Prove(testWitness); 
            }

            sw.Stop();
            var elapsedMillisecondsProve = sw.ElapsedMilliseconds;

            sw = Stopwatch.StartNew();

            for (int i = 0; i < numTest; i++)
            {
                var verifyTest = crs.Verify(testProofs[i]);
                if (!verifyTest)
                {
                    Console.WriteLine("didn't verify");
                }
            }
            sw.Stop();

            Console.WriteLine("PROFILING:");
            Console.WriteLine($"MakeProof:\ntotal time: {elapsedMillisecondsProve}ms\nTime per op: {(double)elapsedMillisecondsProve/(double)numTest}ms");
            Console.WriteLine($"VerifyPrf:\ntotal time: {sw.ElapsedMilliseconds}ms\nTime per op: {(double)sw.ElapsedMilliseconds/(double)numTest}ms");

            var proof = crs.Prove(witness);
            //Console.WriteLine(proof.ToString());
            var verify = crs.Verify(proof);

            if (verify)
            {
                Console.WriteLine("good times");
            }

            var witness2 = sp.Execute(new List<BigInteger> { 900 });
            var proof2 = crs.Prove(witness2);
            var verify2 = crs.Verify(proof2);

            if (verify2)
            {
                Console.WriteLine("good times again");
            }*/

            //var xxx = GenPrime.GetPrime(256);
            //Console.WriteLine(xxx.ToString());
            //var xx = new R1CSReader();
            var src = "template N2Bitvec(n) {\n\tsignal input in;\n\tsignal output out[n];\n\n\tvar lc1 = 0;\n\tvar e2 = 1;\n\tfor (var i = 0; i < n; i++) {\n\t\tout[i] <-- (in >> i) & 1;\n\t\tout[i] * (out[i] - 1) === 0; // alternatively, out[i] * out[i] = out[i]\n\t\tlc1 += out[i] * e2;\n\t\te2 = 2*e2;\n\t}\n\tlc1 === in;\n}\n\ncomponent main {public [in]} = N2Bitvec(32);\n";
            var sc = new SourceReader(src, false);
            var ctok = new CToken(0, 0, Token.Undefined);
            while (ctok.Tok != Token.EOF)
            {
                ctok = sc.Next();
                Console.WriteLine($"[{ctok.Line}:{ctok.Col}] {ctok.Tok} {ctok.Val}");
            }

            var parser = new Parser(src, false);
            Console.WriteLine(parser.Parse().ToString());
        }
    }
}