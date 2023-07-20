using BN256Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CryptoLib2
{
    internal class QAP
    {
        public BigInteger[][] A;
        public BigInteger[][] B;
        public BigInteger[][] C;

        private BigInteger[] Z;

        public QAP() { }

        public static BigInteger[] Interpolate(BigInteger[] xs, BigInteger[] ys)
        {
            var nump = xs.Length;
            BigInteger[] poly = new BigInteger[nump];
            BigInteger[] terms = new BigInteger[nump];

            for (int i = 0; i < nump; i++)
            {
                BigInteger prod = BigInteger.One;
                for (int j = 0; j < nump; j++) terms[j] = BigInteger.Zero;

                for (int j = 0; j < nump; j++)
                {
                    if (i == j) continue;

                    prod *= (xs[i] - xs[j]);
                }

                // invert and mult by ys[i] 
                BigInteger invProd = BigInteger.ModPow(prod, Constants.Order - 2, Constants.Order);
                prod = invProd * ys[i];
                terms[0] = prod;
                for (int j = 0; j < nump; j++)
                {
                    if (i == j) continue;
                    for (int k = nump - 1; k > 0; k--)
                    {
                        terms[k] += terms[k - 1];
                        terms[k - 1] *= -xs[j];
                    }
                }

                for (int j = 0; j < nump; j++)
                {
                    poly[j] += terms[j];
                }
            }

            // reduce
            for (int i = 0; i < nump; i++)
            {
                poly[i] %= Constants.Order;
                if (poly[i] < 0) poly[i] += Constants.Order;
            }

            return poly;
        }

        public QAP(R1CS r1cs)
        {
            var numCoeffs = r1cs.lines.Count;
            var numPolys = r1cs.lines[0].C.Length;

            A = new BigInteger[numPolys][];
            B = new BigInteger[numPolys][];
            C = new BigInteger[numPolys][];

            // transpose
            var At = new BigInteger[numPolys][];
            var Bt = new BigInteger[numPolys][];
            var Ct = new BigInteger[numPolys][];
            for (int i = 0; i < numPolys; i++)
            {
                At[i] = new BigInteger[numCoeffs];
                Bt[i] = new BigInteger[numCoeffs];
                Ct[i] = new BigInteger[numCoeffs];
                for (int j = 0; j < numCoeffs; j++)
                {
                    At[i][j] = r1cs.lines[j].A[i];
                    Bt[i][j] = r1cs.lines[j].B[i];
                    Ct[i][j] = r1cs.lines[j].C[i];
                }
            }

            // interpolate
            BigInteger[] xs = Enumerable.Range(1, numCoeffs).Select(x => new BigInteger(x)).ToArray();
            for (int i = 0; i < numPolys; i++) A[i] = Interpolate(xs, At[i]);
            for (int i = 0; i < numPolys; i++) B[i] = Interpolate(xs, Bt[i]);
            for (int i = 0; i < numPolys; i++) C[i] = Interpolate(xs, Ct[i]);
        }

        public BigInteger EvaluateA(int poly, BigInteger p)
        {
            return EvaluatePoly('a', poly, p);
        }

        public BigInteger EvaluateB(int poly, BigInteger p)
        {
            return EvaluatePoly('b', poly, p);
        }

        public BigInteger EvaluateC(int poly, BigInteger p)
        {
            return EvaluatePoly('c', poly, p);
        }

        private BigInteger EvaluatePoly(char pol, int poly, BigInteger p)
        {
            if (p < 0) throw new ArgumentOutOfRangeException(nameof(p));
            if (p >= Constants.Order) throw new ArgumentOutOfRangeException(nameof(p));

            if (poly < 0 || poly >= A.Length) throw new ArgumentOutOfRangeException(nameof(poly));

            return pol switch
            {
                'a' => A[poly].Reverse().Aggregate(new BigInteger(0), (ans, y) => (ans * p + y) % Constants.Order),
                'b' => B[poly].Reverse().Aggregate(new BigInteger(0), (ans, y) => (ans * p + y) % Constants.Order),
                'c' => C[poly].Reverse().Aggregate(new BigInteger(0), (ans, y) => (ans * p + y) % Constants.Order),
                _ => throw new ArgumentException("internal error", nameof(pol)),
            };
        }

        public void VerifyCorrectness(R1CS r1cs)
        {
            var evalAt = Enumerable.Range(1, r1cs.lines[0].C.Length).Select(x => new BigInteger(x)).ToArray();
            for (int j = 0; j < A[0].Length; j++)
            {
                for (int i = 0; i < A[0].Length; i++)
                {
                    if (EvaluateA(i, j + 1) != r1cs.lines[j].A[i]) throw new Exception($"A({evalAt[i]}) is not equal to {r1cs.lines[j].A[i]} (calculated value: {EvaluateA(i, evalAt[i])})");
                    if (EvaluateB(i, j + 1) != r1cs.lines[j].B[i]) throw new Exception($"B({evalAt[i]}) is not equal to {r1cs.lines[j].B[i]} (calculated value: {EvaluateB(i, evalAt[i])})");
                    if (EvaluateC(i, j + 1) != r1cs.lines[j].C[i]) throw new Exception($"C({evalAt[i]}) is not equal to {r1cs.lines[j].C[i]} (calculated value: {EvaluateC(i, evalAt[i])})");
                }
            }
        }

        public void VerifyCorrectnessWithPrint(R1CS r1cs)
        {
            var evalAt = Enumerable.Range(1, r1cs.lines[0].C.Length).Select(x => new BigInteger(x)).ToArray();
            StringBuilder s = new("VERIFY\n");
            for (int j = 0; j < A[0].Length; j++)
            {
                s.Append($"operation {j}\n");
                StringBuilder ra = new StringBuilder("A: [");
                StringBuilder rb = new StringBuilder("B: [");
                StringBuilder rc = new StringBuilder("C: [");
                for (int i = 0; i < A.Length; i++)
                {
                    //ra.Append($"{A[i].Aggregate(new BigInteger(0), (a, x) => a + x)}");
                    ra.Append($"{EvaluateA(i, j + 1)}");
                    rb.Append($"{EvaluateB(i, j + 1)}");
                    rc.Append($"{EvaluateC(i, j + 1)}");

                    if (i < A.Length - 1)
                    {
                        ra.Append($", ");
                        rb.Append($", ");
                        rc.Append($", ");
                    }
                }

                ra.Append("]\n");
                rb.Append("]\n");
                rc.Append("]\n");

                s.Append(ra.ToString());
                s.Append(rb.ToString());
                s.Append(rc.ToString());
            }

            Console.WriteLine(s.ToString());
        }

        public bool TryVerifyCorrectness(R1CS r1cs)
        {
            try
            {
                VerifyCorrectness(r1cs);
            }
            catch
            {
                return false;
            }

            return true;
        }

        public (BigInteger[] H, BigInteger[] remainder) CalculatePoly(BigInteger[] witness)
        {
            // verify witness is right length
            if (witness.Length != A.Length) throw new ArgumentException();

            int numcoeffs = A[0].Length;
            int numpolys = A.Length;
            BigInteger[] Aw = new BigInteger[numcoeffs];
            BigInteger[] Bw = new BigInteger[numcoeffs];
            BigInteger[] Cw = new BigInteger[numcoeffs];

            // Compute Aw = A.w, Bw = B.w, Cw = C.w
            for (int i = 0; i < numcoeffs; i++)
            {
                Aw[i] = Enumerable.Range(0, numpolys).Select(x => (witness[x] * A[x][i]) % Constants.Order).Aggregate(new BigInteger(0), (x, acc) => (x + acc) % Constants.Order);
                Bw[i] = Enumerable.Range(0, numpolys).Select(x => (witness[x] * B[x][i]) % Constants.Order).Aggregate(new BigInteger(0), (x, acc) => (x + acc) % Constants.Order);
                Cw[i] = Enumerable.Range(0, numpolys).Select(x => (witness[x] * C[x][i]) % Constants.Order).Aggregate(new BigInteger(0), (x, acc) => (x + acc) % Constants.Order);
            }

            // calculate Aw * Bw - Cw
            BigInteger[] Pw = Enumerable.Repeat(BigInteger.Zero, 2 * numcoeffs - 1).ToArray();
            for (int i = 0; i < numcoeffs; i++)
            {
                for (int j = 0; j < numcoeffs; j++)
                {
                    Pw[i + j] += Aw[i] * Bw[j];
                }
            }

            for (int i = 0; i < numcoeffs; i++)
            {
                Pw[i] -= Cw[i];
            }

            // reduce Pw
            for (int i = 0; i < Pw.Length; i++)
            {
                Pw[i] %= Constants.Order;
                if (Pw[i] < 0) Pw[i] += Constants.Order;
            }

            var Z = GetZ(numcoeffs);

            // compute H: the division of P by Z
            BigInteger[] H = new BigInteger[Pw.Length - Z.Length + 1];
            List<BigInteger> rem = Pw.Select(x => x).ToList();
            while (rem.Count >= Z.Length)
            {
                BigInteger lf = (BigInteger.ModPow(Z[^1], Constants.Order - 2, Constants.Order) * rem[^1]) % Constants.Order;
                int pos = rem.Count - Z.Length;
                H[pos] = lf;

                for (int i = pos; i < rem.Count - 1; i++)
                {
                    rem[i] -= Z[i - pos] * lf;
                    rem[i] %= Constants.Order;
                    if (rem[i] < 0) rem[i] += Constants.Order;
                }
                rem.RemoveAt(rem.Count - 1);
            }

            // TODO: implement division by Z and return H
            return (H, rem.ToArray());
        }

        public static BigInteger[] GetZ(int numConstraints)
        {
            // compute Z, order (numcoeffs) poly (so total coeffs is numcoeffs + 1)
            // Z = (x-1)...(x-n) for n = numcoeffs
            BigInteger[] Z = new BigInteger[numConstraints + 1];
            Z[0] = BigInteger.One;
            // note that the coefficients of Z are (-1)^(k-1)*s(numcoeffs + 1, k) for k from 1 to numcoeffs + 1; i.e. the stirling numbers of the first kind (and an alternating sign).
            // this is under the group Z/pZ still, so careful.
            for (int i = 1; i < Z.Length; i++)
            {
                BigInteger[] shift = Enumerable.Range(0, i).Select(x => Z[x]).Prepend(new BigInteger(0)).ToArray();
                for (int j = 0; j < i + 1; j++)
                {
                    Z[j] *= -i;
                    Z[j] += shift[j];
                    Z[j] %= Constants.Order;
                    if (Z[j] < 0) Z[j] += Constants.Order;
                }
            }

            return Z;
        }

        public BigInteger EvaluateZ(BigInteger x)
        {
            if (Z == null) Z = GetZ(A[0].Length);
            return Z.Reverse().Aggregate(new BigInteger(0), (ans, y) => (ans * x + y) % Constants.Order);
        }

        public static bool NoRemainder(BigInteger[] rem)
        {
            return rem.All(x => x.IsZero);
        }
    }
}
