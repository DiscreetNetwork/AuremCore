using Aurem.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Linear
{
    /// <summary>
    /// A class responsible for iterating units on level via a common random permutation.
    /// </summary>
    public class CommonRandomPermutation
    {
        private IDag _dag;
        private IRandomSource _randomSource;
        private ushort _crpFixedPrefix;

        public CommonRandomPermutation(IDag dag, IRandomSource randomSource, ushort crpFixedPrefix)
        {
            _dag = dag;
            _randomSource = randomSource;
            _crpFixedPrefix = crpFixedPrefix;
        }

        /// <summary>
        /// Iterates over all the prime units on a given level in random order.
        /// It calls the given work function on each of the units until the function returns false or the contents run out.
        /// <br /> The underlying random permutation of units is generated in two steps:
        /// <list type="number">
        /// <item>The prefix is based only on the previous timing unit and hashes of units.</item>
        /// <item>The suffix is computed using the random source.</item>
        /// </list>
        /// </summary>
        /// <param name="level"></param>
        /// <param name="previousTU"></param>
        /// <param name="work"></param>
        /// <returns><see langword="false"/> when generating the suffix of the permutation failed due to the dag not reaching a level high enough to reveal the randomBytes needed, or <see langword="true"/> otherwise.</returns>
        public bool CRPIterate(int level, IUnit previousTU, Func<IUnit, bool> work)
        {
            (var prefix, var suffix) = SplitProcesses(_dag.NProc(), _crpFixedPrefix, level, previousTU);

            var perm = DefaultPermutation(_dag, level, prefix);
            foreach (var u in perm)
            {
                if (!work(u)) return true;
            }

            (perm, bool ok) = RandomPermutation(_randomSource, _dag, level, suffix);
            if (!ok) return false;

            foreach (var u in perm)
            {
                if (!work(u)) return true;
            }

            return true;
        }

        private (ushort[], ushort[]) SplitProcesses(ushort nProc, ushort prefixLen, int level, IUnit tu)
        {
            if (prefixLen > nProc)
            {
                prefixLen = nProc;
            }

            var pids = new ushort[nProc];
            for (int i = 0; i < nProc; i++)
            {
                pids[i] = (ushort)((i + level) % nProc);
            }

            if (tu == null)
            {
                return (pids[0..prefixLen], pids[prefixLen..]);
            }

            for (int i = 0; i < nProc; i++)
            {
                pids[i] = (ushort)((pids[i] + tu.Creator()) % nProc);
            }

            return (pids[0..prefixLen], pids[prefixLen..]);
        }

        private List<IUnit> DefaultPermutation(IDag dag, int level, ushort[] pids)
        {
            var permutation = new List<IUnit>();
            foreach (var pid in pids)
            {
                permutation.AddRange(dag.UnitsOnLevel(level).Get(pid));
            }

            permutation.Sort((u, v) => Hash.Compare(u.Hash(), v.Hash()));
            return permutation;
        }

        private (List<IUnit>?, bool) RandomPermutation(IRandomSource rs, IDag dag, int level, ushort[] pids)
        {
            var permutation = new List<IUnit>();
            var priority = new Dictionary<IUnit, byte[]>();

            var allUnitsOnLevel = dag.UnitsOnLevel(level);
            foreach (var pid in pids)
            {
                var units = allUnitsOnLevel.Get(pid);
                if (units.Count == 0) continue;

                var randomBytes = rs.RandomBytes(pid, level + 5);
                if (randomBytes == null || randomBytes.Length == 0) return (null, false);

                // It is risky to directly append to this returned value, so we need to copy first
                var tmp = new byte[randomBytes.Length];
                Array.Copy(randomBytes, tmp, randomBytes.Length);
                randomBytes = tmp;
                var rbLen = randomBytes.Length;

                foreach (var u in units)
                {
                    var rbytes = randomBytes.Take(rbLen).Concat(u.Hash().Data).ToArray();
                    priority[u] = SHA256.HashData(rbytes);
                }

                permutation.AddRange(units);
            }

            permutation.Sort((u, v) =>
            {
                if (priority[u] == null) return -1;
                if (priority[v] == null) return 1;
                if (priority[u].Length < priority[v].Length) return -1;
                if (priority[u].Length > priority[v].Length) return 1;

                for (int i = 0; i < priority[u].Length; i++)
                {
                    if (priority[u][i] < priority[v][i]) return -1;
                    if (priority[u][i] > priority[v][i]) return 1;
                }

                throw new Exception("two elements have equal priority");
            });

            return (permutation, true);
        }
    }
}
