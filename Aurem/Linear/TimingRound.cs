using Aurem.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Linear
{
    /// <summary>
    /// Represents a single round of ordered units.
    /// </summary>
    public class TimingRound
    {
        public IUnit CurrentTU;
        public IUnit[] LastTUs;

        public TimingRound(IUnit currentTimingUnit, IList<IUnit> lastTimingUnits)
        {
            CurrentTU = currentTimingUnit;
            LastTUs = lastTimingUnits.ToArray();
        }

        /// <summary>
        /// Returns all units ordered in this timing round.
        /// </summary>
        /// <returns></returns>
        public IList<IUnit> OrderedUnits()
        {
            var layers = GetAntichainLayers(CurrentTU, LastTUs);
            var sortedUnits = MergeLayers(layers);
            return sortedUnits;
        }

        /// <summary>
        /// We can prove that comparing with last k timing units (for k as first round where the deterministic common vote is zero) is enough to verify if a unit is already ordered.
        /// Since the common vote for round k is zero, every unit on level tu.Level()+k must be above a timing unit tu; otherwise some unit would decide zero for it.
        /// </summary>
        /// <param name="u"></param>
        /// <param name="prevTUs"></param>
        /// <returns></returns>
        public bool CheckIfAlreadyOrdered(IUnit u, IList<IUnit> prevTUs)
        {
            var prevTU = prevTUs.Last();
            if (prevTU == null || u.Level() > prevTU.Level())
            {
                return false;
            }

            for (var it = prevTUs.Count - 1; it >= 0; it--)
            {
                if (IUnit.Above(prevTUs[it], u)) return true;
            }

            return false;
        }

        /// <summary>
        /// For a given timing unit tu, returns all the units in its timing round divided into layers.
        /// <br /> The zero-th layer is formed by minimal units in this timing round.
        /// <br /> The first layer is formed by minimal units when the zero-th layer is removed.
        /// </summary>
        /// <param name="tu"></param>
        /// <param name="prevTUs"></param>
        /// <returns></returns>
        public List<List<IUnit>> GetAntichainLayers(IUnit tu, IList<IUnit> prevTUs)
        {
            var unitToLayer = new Dictionary<Hash, int>();
            var seenUnits = new Dictionary<Hash, bool>();
            var result = new List<List<IUnit>>();

            void dfs(IUnit u)
            {
                seenUnits[u.Hash()] = true;
                var minLayerBelow = -1;
                foreach (var uParent in u.Parents())
                {
                    if (uParent == null) continue;
                    if (CheckIfAlreadyOrdered(uParent, prevTUs)) continue;

                    var success = seenUnits.TryGetValue(uParent.Hash(), out var seenUnitsVal);
                    if (!success) seenUnitsVal = false;
                    if (!seenUnitsVal) dfs(uParent);


                    var success2 = unitToLayer.TryGetValue(uParent.Hash(), out var unitToLayerVal);
                    if (!success2) unitToLayerVal = 0;
                    if (unitToLayerVal > minLayerBelow) minLayerBelow = unitToLayerVal;
                }

                var uLayer = minLayerBelow + 1;
                unitToLayer[u.Hash()] = uLayer;
                if (result.Count <= uLayer)
                {
                    result.Add(new List<IUnit> { u });
                }
                else
                {
                    result[uLayer].Add(u);
                }
            };

            dfs(tu);
            return result;
        }

        public List<IUnit> MergeLayers(List<List<IUnit>> layers)
        {
            Hash totalXOR = new Hash(new byte[32]);
            for (int i = 0; i < layers.Count; i++)
            {
                foreach (var u in layers[i]) totalXOR.XOREqual(u.Hash());
            }

            // tiebreaker is a map from units to its tiebreaker value
            var tiebreaker = new Dictionary<Hash, Hash>();
            for (int l = 0; l < layers.Count; l++)
            {
                foreach (var u in layers[l]) tiebreaker[u.Hash()] = Hash.XOR(totalXOR, u.Hash());
            }

            var sortedUnits = new List<IUnit>();

            for (int l = 0; l < layers.Count; l++)
            {
                layers[l].Sort((x, y) => 
                { 
                    var tbi = tiebreaker[x.Hash()];
                    var tbj = tiebreaker[y.Hash()];

                    return Hash.Compare(tbi, tbj);
                });
                sortedUnits.AddRange(layers[l]);
            }

            return sortedUnits;
        }
    }
}
