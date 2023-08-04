using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuremCore.Crypto.Threshold;

namespace Aurem.Model
{
    /// <summary>
    /// Unit is the main interface for unit that knows the list of its parents.
    /// </summary>
    public interface IUnit : IPreunit
    {
        /// <summary>
        /// Returns the parents of this unit.
        /// </summary>
        /// <returns></returns>
        public IList<IUnit> Parents();

        /// <summary>
        /// Returns the level of this unit in the dag, as defined in the protocol paper.
        /// </summary>
        /// <returns></returns>
        public int Level();

        /// <summary>
        /// Checks if this unit is above the given unit produced by the same creator.
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        public bool AboveWithinProc(IUnit unit);

        /// <summary>
        /// Floor returns a slice of maximal units created by the given process that are strictly below this unit.
        /// </summary>
        /// <param name="id">the procID to use.</param>
        /// <returns></returns>
        public IEnumerable<IUnit> Floor(ushort id);

        /// <summary>
        /// Checks if unit u is above unit v.
        /// </summary>
        /// <param name="u"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public static bool Above(IUnit u, IUnit v)
        {
            if (u == null || v == null) return false;

            if (u.Equals(v)) return true;

            foreach (var w in u.Floor(v.Creator())) {
                if (w.AboveWithinProc(v)) return true;
            }

            return false;
        }

        /// <summary>
        /// Calculates the level of a unit having the given set of parents.
        /// </summary>
        /// <param name="parents"></param>
        /// <returns></returns>
        public static int LevelFromParents(IEnumerable<IUnit> parents)
        {
            var _parents = parents.ToList();
            ushort nproc = (ushort)_parents.Count;
            int level = 0;
            ushort onlevel = 0;

            foreach (var p in _parents)
            {
                if (p == null) continue;

                if (p.Level() == level)
                {
                    onlevel++;
                }
                else if (p.Level() > level)
                {
                    onlevel = 1;
                    level = p.Level();
                }
            }

            if (onlevel >= TUtil.MinimalQuorum(nproc))
            {
                level++;
            }

            return level;
        }

        /// <summary>
        /// Computes all maximal units produced by pid present in their parents and their floors.
        /// </summary>
        /// <param name="parents"></param>
        /// <param name="pid"></param>
        /// <returns></returns>
        public static IEnumerable<IUnit> MaximalByPid(IEnumerable<IUnit> parents, ushort pid)
        {
            var _parents = parents.ToArray();

            if (_parents[pid] == null) return Enumerable.Empty<IUnit>();

            var maximal = new List<IUnit> { _parents[pid] };

            foreach (var p in _parents)
            {
                if (p == null) continue;
                foreach (var w in p.Floor(pid))
                {
                    var found = false;
                    var ri = -1;

                    for (int ix = 0; ix < maximal.Count; ix++)
                    {
                        if (Above(w, maximal[ix]))
                        {
                            found = true;
                            ri = ix;

                            // loop can be broken since if we find other index storing 'w' it's a proof of self-forking
                            break;
                        }

                        if (Above(maximal[ix], w))
                        {
                            found = true;

                            // loop can be broken since if 'w' is above another index that contradicts the assumption that elements of 'floors' (narrowed to some index) are not comparable
                            break;
                        }
                    }

                    if (!found)
                    {
                        maximal.Add(w);
                    }
                    else if (ri >= 0)
                    {
                        maximal[ri] = w;
                    }
                }
            }

            return maximal;
        }

        /// <summary>
        /// Returns the predecessor of a unit, which is the parent created by the same process of this unit.
        /// </summary>
        /// <returns></returns>
        public IUnit Predecessor()
        {
            return Parents().ToList()[Creator()];
        }

        /// <summary>
        /// Checks whether `u` is below any of the units in `us`.
        /// </summary>
        /// <returns></returns>
        public static bool BelowAny(IUnit u, IEnumerable<IUnit> us) => us.Any(v => v != null && Above(v, u));

        /// <summary>
        /// Converts a selection of units into a selection of hashes.
        /// </summary>
        /// <param name="units"></param>
        /// <returns></returns>
        public static IEnumerable<Hash> ToHashes(IEnumerable<IUnit> units)
        {
            var enumerator = units.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current != null) yield return enumerator.Current.Hash();
                else yield return Model.Hash.Empty;
            }
        }
    }
}
