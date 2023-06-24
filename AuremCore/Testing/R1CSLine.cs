using AuremCore.Crypto.BN256;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Testing
{
    internal class R1CSLine
    {
        public BigInteger[] A;
        public BigInteger[] B;
        public BigInteger[] C;

        public R1CSLine()
        {
        }

        public static string BIAtoS(BigInteger[] a)
        {
            StringBuilder rv = new StringBuilder();
            for (int i = 0; i < a.Length; i++)
            {
                rv.Append(a[i].ToString());
                if (i < a.Length - 1)
                {
                    rv.Append(", ");
                }
            }

            return rv.ToString();
        }

        public static string BIAtoS2(BigInteger[] a)
        {
            StringBuilder rv = new StringBuilder();
            bool pZ = false;
            bool ellipses = false;
            for (int i = 0; i < a.Length; i++)
            {
                bool negOne = a[i] == BigInteger.Parse("21888242871839275222246405745257275088548364400416034343698204186575808495616");
                bool pow2 = a[i].IsPowerOfTwo && a[i] > 2;

                var astr = negOne ? "-1" : (pow2 ? $"2**{a[i].GetBitLength()-1}" : a[i].ToString());

                if (!pZ && a[i].IsZero)
                {
                    pZ = true;
                    if (i == 0) rv.Append('0');
                }
                else if (pZ && !ellipses && a[i].IsZero)
                {
                    ellipses = true;
                    rv.Append("... ");
                }
                else if (pZ && ellipses && a[i].IsZero)
                {
                    continue;
                }
                else if (i == 0)
                {
                    rv.Append(astr);
                    rv.Append(' ');
                }
                else if (i == 1)
                {
                    rv.Append($"y({astr}) ");
                }
                else if (i == 2)
                {
                    rv.Append($"a({astr})");
                }
                else if (i == 3)
                {
                    rv.Append($"b({astr}) ");
                }
                else
                {
                    rv.Append($"sym{i-4}({astr}) ");
                }
            }

            return rv.ToString();
        }

        public static string BIAtoSBits(BigInteger[] a)
        {
            StringBuilder rv = new StringBuilder();
            bool pZ = false;
            bool ellipses = false;
            for (int i = 0; i < a.Length; i++)
            {
                bool negOne = a[i] == BigInteger.Parse("21888242871839275222246405745257275088548364400416034343698204186575808495616");
                bool pow2 = a[i].IsPowerOfTwo && a[i] > 2;

                var astr = negOne ? "-1" : (pow2 ? $"2**{a[i].GetBitLength() - 1}" : a[i].ToString());

                if (!pZ && a[i].IsZero)
                {
                    pZ = true;
                    if (i == 0) rv.Append('0');
                }
                else if (pZ && !ellipses && a[i].IsZero)
                {
                    ellipses = true;
                    rv.Append("... ");
                }
                else if (pZ && ellipses && a[i].IsZero)
                {
                    continue;
                }
                else if (i == 0)
                {
                    rv.Append(astr);
                    rv.Append(' ');
                }
                else if (i == 1)
                {
                    rv.Append($"y({astr}) ");
                }
                else if (i == 2)
                {
                    rv.Append($"a({astr})");
                }
                else if (i - 3 >= 0 && i - 3 < 32)
                {
                    rv.Append($"a_{35 - i}({astr})");
                }
                else if (i == 35)
                {
                    rv.Append($"b({astr}) ");
                }
                else if (i - 36 >= 0 && i - 36 < 32)
                {
                    rv.Append($"b_{68 - i}({astr})");
                }
                else if (i - 68 >= 0 && i - 68 < 32)
                {
                    rv.Append($"y_{100 - i}({astr})");
                }
                else
                {
                    rv.Append($"sym{i - 100}({astr}) ");
                }
            }

            return rv.ToString();
        }

        public override string ToString()
        {
            return $"A: [{BIAtoS(A)}]\nB: [{BIAtoS(B)}]\nC: [{BIAtoS(C)}]\n";
        }

        public string ToString2()
        {
            return $"A: [{BIAtoS2(A)}]\nB: [{BIAtoS2(B)}]\nC: [{BIAtoS2(C)}]\n";
        }

        public string ToStringBits()
        {
            return $"A: [{BIAtoSBits(A)}]\nB: [{BIAtoSBits(B)}]\nC: [{BIAtoSBits(C)}]\n";
        }

        public R1CSLine(Symbol[] syms, Operation op) : this()
        {
            A = new BigInteger[syms.Length];
            B = new BigInteger[syms.Length];
            C = new BigInteger[syms.Length];

            for (int i = 0; i < syms.Length; i++)
            {
                A[i] = BigInteger.Zero;
                B[i] = BigInteger.Zero;
                C[i] = BigInteger.Zero;
            }

            switch (op.op)
            {
                case Op.ASSIGN:
                    // A = [0... rhs ...0]; B = [1 ... 0]; C = [0... lhs ...0]
                    B[0] = BigInteger.One;
                    if (op.rhs1.constant)
                    {
                        A[0] = op.rhs1.value;
                    }
                    else
                    {
                        A[op.rhs1.offset] = BigInteger.One;
                    }
                    C[op.lhs.offset] = BigInteger.One;
                    break;
                case Op.ADD:
                    // A = [0... rhs1 ... rhs2 ... 0]; B = [1 ... 0]; C = [0... lhs ... 0]
                    // this assumes rhs1 and rhs2 are different
                    B[0] = BigInteger.One;
                    if (op.rhs1.constant)
                    {
                        A[0] = op.rhs1.value;
                    }
                    else
                    {
                        A[op.rhs1.offset] = BigInteger.One;
                    }
                    if (op.rhs2.constant)
                    {
                        if (!op.rhs1.constant)
                        {
                            A[0] = op.rhs2.value;
                        }
                        else
                        {
                            C[0] = -op.rhs2.value;
                        }
                    }
                    else
                    {
                        if (op.rhs1.offset == op.rhs2.offset)
                        {
                            C[op.rhs2.offset] = -BigInteger.One;
                        }
                        else
                        {
                            A[op.rhs2.offset] = BigInteger.One;
                        }
                    }
                    C[op.lhs.offset] = BigInteger.One;
                    break;
                case Op.SUB:
                    throw new NotImplementedException();
                case Op.MUL:
                    if (op.rhs1.constant) A[0] = op.rhs1.value;
                    else A[op.rhs1.offset] = BigInteger.One;
                    if (op.rhs2.constant) B[0] = op.rhs2.value;
                    else B[op.rhs2.offset] = BigInteger.One;
                    C[op.lhs.offset] = BigInteger.One;
                    break;
            }
        }

        public R1CSLine Simplify()
        {
            // simplify converts all negative coefficients to their respective additive inverse under Z/pZ
            // also ensures constants are modulo order
            for (int i = 0; i < A.Length; i++)
            {
                while (A[i] < 0) A[i] += Constants.Order;
                if (A[i] >= Constants.Order) A[i] %= Constants.Order;

                while (B[i] < 0) B[i] += Constants.Order;
                if (B[i] >= Constants.Order) B[i] %= Constants.Order;

                while (C[i] < 0) C[i] += Constants.Order;
                if (C[i] >= Constants.Order) C[i] %= Constants.Order;
            }

            return this;
        }
    }
}
