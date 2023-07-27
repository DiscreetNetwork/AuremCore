using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Model
{
    /// <summary>
    /// Crown represents nproc units created by different processes in a condensed form. 
    /// It contains heights of the units and a combined hash of the units (the ControlHash). 
    /// Any missing unit is represented by height -1 and the zero hash.
    /// </summary>
    public class Crown
    {
        /// <summary>
        /// The heights of the units represented by this crown.
        /// </summary>
        public int[] Heights { get; set; }

        /// <summary>
        /// The combined hash of the units.
        /// </summary>
        public Hash ControlHash { get; set; }

        /// <summary>
        /// Creates a crown with all units missing.
        /// </summary>
        /// <param name="nproc">Number of processes to represent units of.</param>
        public Crown(ushort nproc) 
        {
            Heights = new int[nproc];
            for (int i = 0; i < Heights.Length; i++)
            {
                Heights[i] = -1;
            }

            ControlHash = Hash.CombineHashes(Enumerable.Repeat(Hash.ZeroHash, nproc));
        }


        /// <summary>
        /// Creates a crown with the given collection of heights and given control hash.
        /// </summary>
        /// <param name="heights">The collection of heights to be used in this crown.</param>
        /// <param name="controlHash">The control hash corresponding to this crown.</param>
        public Crown(int[] heights, Hash controlHash)
        {
            Heights = heights;
            ControlHash = controlHash;
        }

        /// <summary>
        /// Creates a crown consisting of the given collection of units. 
        /// It assumes that the given collection of parents represents the number of processes (nproc), and the nth unit is created by the nth process (i.e., ordered).
        /// </summary>
        /// <param name="parents">The parent units to be represented by this crown.</param>
        public Crown(List<IUnit> parents)
        {
            var nproc = parents.Count;
            Heights = new int[nproc];
            var hashes = new Hash[nproc];

            for (int i = 0; i < nproc; i++)
            {
                if (parents[i] == null)
                {
                    Heights[i] = -1;
                    hashes[i] = Hash.ZeroHash;
                }
                else
                {
                    Heights[i] = parents[i].Height();
                    hashes[i] = parents[i].Hash();
                }
            }

            ControlHash = Hash.CombineHashes(hashes);
        }
    }
}
