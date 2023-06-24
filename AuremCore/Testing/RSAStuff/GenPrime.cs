using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Testing.RSAStuff
{
    internal class GenPrime
    {
        private static BigInteger[] primes = Sieve(100000).Select(x => new BigInteger(x)).ToArray();

        private static IEnumerable<int> Sieve(int bound)
        {
            var notPrime = new bool[bound];
            int checkBound = (int)Math.Ceiling(Math.Sqrt(bound));

            for (int i = 2; i <= checkBound; i++)
            {
                if (notPrime[i]) continue;

                yield return i;

                for (int j = i; j < bound; j += i) notPrime[i] = true;
            }

            for (int i = checkBound + 1; i < bound; i++)
            {
                if (!notPrime[i]) yield return i;
            }
        }

        private static bool SimpleCheck(BigInteger pr)
        {
            return primes.All(x => (pr % x) != 0);
        }

        public static BigInteger GetPrime(int nBytes)
        {
            Random rnd = new Random();
            byte[] pdat = new byte[nBytes + 1];
            rnd.NextBytes(pdat);
            pdat[nBytes] = 0; // positive
            pdat[0] |= 1;  // odd

            BigInteger pr = new BigInteger(pdat);
        redo:
            pr += 2;

            // the average probability of success is (1/4)^t, for t tests.
            // in practice, this is actually closer to (1/8)^t.
            // so for a provable prime with error <2^(-100), we want nBytes*4 + 50 tests.

            if (!SimpleCheck(pr)) goto redo;

            for (int i = 0; i < nBytes*4+50; i++)
            {
                var r = pr - 1;
                BigInteger s = 0;
                while ((r & 1) == 0)
                {
                    r >>= 1;
                    s++;
                }

                var isOne = BigInteger.ModPow(primes[i], r, pr);
                if (isOne != 1 && isOne != pr - 1) goto redo;

                for (int j = 0; j < s; j++)
                {
                    isOne = (isOne * isOne) % pr;
                    if (isOne != 1) goto redo;
                }
            }

            return pr;
        }
    }
}
