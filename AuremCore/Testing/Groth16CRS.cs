using AuremCore.Crypto.BN256;
using AuremCore.Crypto.BN256.Common;
using AuremCore.Crypto.BN256.Extensions;
using AuremCore.Crypto.BN256.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Testing
{
    internal class Groth16CRS
    {
        public int m; // m is the number of total variables
        public int l; // l is the number of public inputs
        public int n; // n is the number of constraints

        public QAP Program;
        public Groth16ProvingKey prove;
        public Groth16VerificationKey verify;

        public Groth16CRS(SimpleProgram prog)
        {
            R1CS r1cs = new R1CS(prog);
            r1cs.Simplify();
            Program = new QAP(r1cs);

            m = Program.C.Length;
            l = prog.Outputs.Count + 1; // add by 1 since 1 is an input
            n = r1cs.lines.Count;

            BigInteger alpha = SecretKey.RandomScalar();
            BigInteger beta = SecretKey.RandomScalar();
            BigInteger gamma = SecretKey.RandomScalar();
            BigInteger delta = SecretKey.RandomScalar();
            BigInteger tau = SecretKey.RandomScalar();

            // construct powers of tau
            BigInteger[] powersOfTau = new BigInteger[n];
            BigInteger tmp = BigInteger.One;
            for (int i = 0; i < n; i++)
            {
                powersOfTau[i] = tmp;
                tmp *= tau;
                tmp %= Constants.Order;
            }

            // construct proving key
            prove = new Groth16ProvingKey();
            prove.g1ByAlpha = new G1().ScalarBaseMult(alpha);
            prove.g1ByBeta = new G1().ScalarBaseMult(beta);
            prove.g1ByDelta = new G1().ScalarBaseMult(delta);

            // g1 powers of tau
            prove.g1ByPowersOfTau = new G1[n];
            for (int i = 0; i < n; i++)
            {
                prove.g1ByPowersOfTau[i] = new G1().ScalarBaseMult(powersOfTau[i]);
            }

            // construct l polynomial stuff
            prove.g1ByLiTauOverDelta = new G1[m - l];
            BigInteger invDelta = BigInteger.ModPow(delta, Constants.Order - 2, Constants.Order);
            invDelta %= Constants.Order;
            if (invDelta < 0) invDelta += Constants.Order;
            for (int i = 0; i < prove.g1ByLiTauOverDelta.Length; i++)
            {
                BigInteger lioft = EvalL(tau, alpha, beta, i + l);
                lioft *= invDelta;
                lioft %= Constants.Order;
                if (lioft < 0) lioft += Constants.Order;
                prove.g1ByLiTauOverDelta[i] = new G1().ScalarBaseMult(lioft);
            }

            // construct Z stuff
            prove.g1ByZTauTimesPTauMinus1 = new G1[n - 1];
            BigInteger ZofTauOverD = (Program.EvaluateZ(tau) * invDelta) % Constants.Order;
            if (ZofTauOverD < 0) ZofTauOverD += Constants.Order;
            for (int i = 0; i < n - 1; i++)
            {
                BigInteger prod = (ZofTauOverD * powersOfTau[i]) % Constants.Order;
                if (prod < 0) prod += Constants.Order;
                prove.g1ByZTauTimesPTauMinus1[i] = new G1().ScalarBaseMult(prod);
            }

            // construct g2 stuff
            prove.g2ByBeta = new G2().ScalarBaseMult(beta);
            prove.g2ByDelta = new G2().ScalarBaseMult(delta);

            // g2 powers of tau
            prove.g2ByPowersOfTau = new G2[n];
            for (int i = 0; i < n; i++)
            {
                prove.g2ByPowersOfTau[i] = new G2().ScalarBaseMult(powersOfTau[i]);
            }

            prove.Program = Program;

            // construct verification key
            verify = new Groth16VerificationKey();
            verify.g1 = Constants.G1Gen;
            verify.g2 = Constants.G2Gen;

            // l stuff again
            verify.g1ByLiTauOverGamma = new G1[l];
            BigInteger invGamma = BigInteger.ModPow(gamma, Constants.Order - 2, Constants.Order);
            invGamma %= Constants.Order;
            if (invGamma < 0) invGamma += Constants.Order;
            for (int i = 0; i < l; i++)
            {
                BigInteger lioft = EvalL(tau, alpha, beta, i);
                lioft *= invGamma;
                lioft %= Constants.Order;
                if (lioft < 0) lioft += Constants.Order;
                verify.g1ByLiTauOverGamma[i] = new G1().ScalarBaseMult(lioft);
            }

            // g2 stuff
            verify.g2ByGamma = new G2().ScalarBaseMult(gamma);
            verify.g2ByDelta = new G2().ScalarBaseMult(delta);
            verify.gtByAlphaTimesBeta = new GT().Pair(prove.g1ByAlpha, prove.g2ByBeta);
            verify.Program = Program;
        }

        private BigInteger EvalL(BigInteger x, BigInteger alpha, BigInteger beta, int poly)
        {
            // Ai(x)
            var Ai = Program.EvaluateA(poly, x);
            var Bi = Program.EvaluateB(poly, x);
            var Ci = Program.EvaluateC(poly, x);

            var rv = Ai * beta + Bi * alpha + Ci;

            rv %= Constants.Order;
            if (rv < 0) rv += Constants.Order;

            return rv;
        }

        private G1 EvalA(BigInteger w, int poly)
        {
            if (poly < 0) throw new ArgumentOutOfRangeException(nameof(poly));
            if (poly >= m) throw new ArgumentOutOfRangeException(nameof(poly));

            G1 res = new G1();
            G1 tmp = new G1();
            for (int i = 0; i < Program.A[poly].Length; i++)
            {
                tmp.Set(prove.g1ByPowersOfTau[i]);
                tmp = tmp.ScalarMult(tmp, Program.A[poly][i]);
                res = res.Add(res, tmp);
            }

            return new G1().ScalarMult(res, w);
        }

        private G2 EvalB(BigInteger w, int poly)
        {
            if (poly < 0) throw new ArgumentOutOfRangeException(nameof(poly));
            if (poly >= m) throw new ArgumentOutOfRangeException(nameof(poly));

            G2 res = new G2();
            G2 tmp = new G2();
            for (int i = 0; i < Program.B[poly].Length; i++)
            {
                tmp.Set(prove.g2ByPowersOfTau[i]);
                tmp = tmp.ScalarMult(tmp, Program.B[poly][i]);
                res = res.Add(res, tmp);
            }

            return new G2().ScalarMult(res, w);
        }

        private G1 EvalB1(BigInteger w, int poly)
        {
            if (poly < 0) throw new ArgumentOutOfRangeException(nameof(poly));
            if (poly >= m) throw new ArgumentOutOfRangeException(nameof(poly));

            G1 res = new G1();
            G1 tmp = new G1();
            for (int i = 0; i < Program.B[poly].Length; i++)
            {
                tmp.Set(prove.g1ByPowersOfTau[i]);
                tmp = tmp.ScalarMult(tmp, Program.B[poly][i]);
                res = res.Add(res, tmp);
            }

            return new G1().ScalarMult(res, w);
        }

        private G1 EvalC(BigInteger w, int poly)
        {
            if (poly < 0) throw new ArgumentOutOfRangeException(nameof(poly));
            if (poly >= m) throw new ArgumentOutOfRangeException(nameof(poly));

            G1 res = new G1();
            G1 tmp = new G1();
            for (int i = 0; i < Program.C[poly].Length; i++)
            {
                tmp.Set(prove.g1ByPowersOfTau[i]);
                tmp = tmp.ScalarMult(tmp, Program.C[poly][i]);
                res = res.Add(res, tmp);
            }

            return new G1().ScalarMult(res, w);
        }

        public Groth16Proof Prove(BigInteger[] witness)
        {
            if (witness == null) throw new ArgumentNullException(nameof(witness));
            if (witness.Length != m) throw new ArgumentException("witness length incorrect", nameof(witness));

            Groth16Proof proof = new Groth16Proof();
            proof.PublicInputs = Enumerable.Range(0, l).Select(x => witness[x]).ToArray();
            Console.WriteLine(R1CSLine.BIAtoS(proof.PublicInputs));

            // generate random r and s
            BigInteger r = SecretKey.RandomScalar();
            BigInteger s = SecretKey.RandomScalar();

            // calculate A
            proof.A = new G1().Set(prove.g1ByAlpha);
            proof.A = proof.A.Add(proof.A, new G1().ScalarMult(prove.g1ByDelta, r));
            for (int i = 0; i < m; i++)
            {
                proof.A = proof.A.Add(proof.A, EvalA(witness[i], i));
            }

            // calculate B
            proof.B = new G2().Set(prove.g2ByBeta);
            proof.B = proof.B.Add(proof.B, new G2().ScalarMult(prove.g2ByDelta, s));
            for (int i = 0; i < m; i++)
            {
                proof.B = proof.B.Add(proof.B, EvalB(witness[i], i));
            }

            // calculate B1
            var B1 = new G1().Set(prove.g1ByBeta);
            B1 = B1.Add(B1, new G1().ScalarMult(prove.g1ByDelta, s));
            for (int i = 0; i < m; i++)
            {
                B1 = B1.Add(B1, EvalB1(witness[i], i));
            }

            // calculate H and H1
            (var H, var rem) = Program.CalculatePoly(witness);
            if (!QAP.NoRemainder(rem)) throw new Exception("shame on you");

            var H1 = new G1();
            var htmp = new G1();
            for (int i = 0; i < H.Length; i++)
            {
                htmp = htmp.Set(prove.g1ByZTauTimesPTauMinus1[i]);
                htmp = htmp.ScalarMult(htmp, H[i]);
                H1 = H1.Add(H1, htmp);
            }

            // calculate C
            proof.C = new G1().Set(H1);
            proof.C = proof.C.Add(proof.C, new G1().ScalarMult(proof.A, s));
            proof.C = proof.C.Add(proof.C, new G1().ScalarMult(B1, r));
            var rdelt = new G1().ScalarMult(prove.g1ByDelta, r);
            rdelt = rdelt.ScalarMult(rdelt, s);
            rdelt = rdelt.Neg(rdelt);
            proof.C = proof.C.Add(proof.C, rdelt);
            for (int i = l; i < m; i++)
            {
                proof.C = proof.C.Add(proof.C, prove.g1ByLiTauOverDelta[i-l]);
            }

            return proof;
        }

        public bool Verify(Groth16Proof proof)
        {
            var AB = new GT().Pair(proof.A, proof.B);

            var Lp = new G1();
            for (int i = 0; i < l; i++)
            {
                Lp = Lp.Add(Lp, new G1().ScalarMult(verify.g1ByLiTauOverGamma[i], proof.PublicInputs[i]));
            }

            var Rhs = new GT().Set(verify.gtByAlphaTimesBeta);
            Rhs = Rhs.Add(Rhs, new GT().Pair(Lp, verify.g2ByGamma));
            Rhs = Rhs.Add(Rhs, new GT().Pair(proof.C, verify.g2ByDelta));

            Console.WriteLine(PrintUtil.Hexify(AB.Marshal(), true));
            Console.WriteLine(PrintUtil.Hexify(Rhs.Marshal(), true));


            return AB.Marshal().BEquals(Rhs.Marshal());
        }
    }
}
