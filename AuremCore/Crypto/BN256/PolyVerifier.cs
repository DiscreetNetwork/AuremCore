using AuremCore.Crypto.BN256.Models;
using AuremCore.Crypto.BN256.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace AuremCore.Crypto.BN256
{
    public class PolyVerifier
    {
        public SecretKey[] Vector;

        public PolyVerifier() { }

        public PolyVerifier(SecretKey[] vector) {  Vector = vector; }

        public bool Verify(VerificationKey[] elems)
        {
            if (elems == null || Vector == null) return false;
            if (Vector.Length != elems.Length) return false;

            G2 scalarProduct = elems.Zip(Vector).AsParallel().Select(i =>
            {
                (var elem, var vec) = i;
                return new G2().ScalarMult(elem.Key, vec);
            }).Aggregate(new G2(), (x, y) => x.Add(x, y));

            byte[] zmarsh = new G2().Marshal();
            byte[] smarsh = scalarProduct.Marshal();

            return zmarsh.BEquals(smarsh);
        }

        public static PolyVerifier New(int n, int f)
        {
            BigInteger[][] newton = new BigInteger[n+1][];
            for (int i = 0; i <= n; i++)
            {
                newton[i] = new BigInteger[i + 1];
                newton[i][0] = new BigInteger(0);
                newton[i][i] = new BigInteger(0);

                for (int j= 1; j < i; j++)
                {
                    newton[i][j] = newton[i - 1][j] + newton[i - 1][j - 1];
                }
            }

            BigInteger[][] sym = new BigInteger[n+1][];
            for (int i = 0; i <= n; i++)
            {
                sym[i] = new BigInteger[i + 1];
                sym[i][0] = new BigInteger();
                for (int j= 1; j <= i; j++)
                {
                    sym[i][j] = new BigInteger(i);
                    sym[i][j] = sym[i][j] * sym[i - 1][j - 1];
                    if (j <= i - 1)
                    {
                        sym[i][j] = sym[i][j] + sym[i - 1][j];
                    }
                }
            }

            BigInteger[][] coeff = new BigInteger[n + 1][];
            for (int i = 0; i <= n; i++)
            {
                coeff[i] = new BigInteger[n];
                coeff[i][0] = new BigInteger(1);
                for (int j = 1; j <= n - 1; j++)
                {
                    coeff[i][j] = new BigInteger(-i);
                    coeff[i][j] = coeff[i][j] * coeff[i][j - 1];
                    coeff[i][j] = coeff[i][j] + sym[n][j];
                }
            }

            BigInteger[][] invV = new BigInteger[n][];
            for (int i = 0; i < n; i++)
            {
                invV[i] = new BigInteger[n];
                for (int j = 0; j < n; j++)
                {
                    invV[i][j] = new BigInteger(1);
                    invV[i][j] = invV[i][j] * newton[n - 1][j];
                    invV[i][j] = invV[i][j] + coeff[j + 1][i];
                }
            }

            BigInteger[] magicVector = new BigInteger[n];
            for (int i = 0; i < n; i++)
            {
                magicVector[i] = new BigInteger(0);
            }

            for (int i = 0; i < n - f - 1; i++)
            {
                BigInteger k = SecretKey.RandomScalar();
                for (int j = 0; j < n; j++)
                {
                    BigInteger term = new BigInteger(1);
                    term = term * invV[i][j];
                    term = term * k;
                    if (j % 2 == 1)
                    {
                        term = -term;
                    }

                    magicVector[j] = magicVector[j] + term;
                }
            }

            SecretKey[] vector = new SecretKey[n];
            for (int i = 0; i < n; i++)
            {
                vector[i] = new SecretKey(magicVector[i] % Constants.Order);
            }

            return new PolyVerifier(vector);
        }
    }
}
