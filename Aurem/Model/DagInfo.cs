using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Model
{
    /// <summary>
    /// Contains information about heights of the most recent units in a DAG.
    /// </summary>
    public class DagInfo
    {
        /// <summary>
        /// The epoch corresponding to the DAG.
        /// </summary>
        public uint Epoch { get; set; }

        /// <summary>
        /// The heights of the most recent units in the DAG, ordered by process.
        /// </summary>
        public int[] Heights { get; set; }

        public static DagInfo MaxView(IDag dag)
        {
            var maxes = dag.MaximalUnitsPerProcess();
            var heights = new List<int>(Enumerable.Repeat(-1, dag.NProc()));
            maxes.Iterate((List<IUnit> units) =>
            {
                var h = -1;
                var c = -1;
                foreach (var u in units)
                {
                    if (c == -1) c = u.Creator();
                    if (u.Height() > h)
                    {
                        h = u.Height();
                    }
                }
                if (c != -1) heights[c] = h;
                return true;
            });

            return new DagInfo { Epoch = dag.EpochID(), Heights = heights.ToArray() };
        }
    }
}
