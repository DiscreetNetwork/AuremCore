using Aurem.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Adding
{
    /// <summary>
    /// A structure that keeps a single preunit waiting to be added to the DAG.
    /// </summary>
    public class WaitingPreunit
    {
        public IPreunit Pu = null!;
        public ulong Id;

        /// <summary>
        /// PID of the process that sent us this preunit.
        /// </summary>
        public ushort Source;

        /// <summary>
        /// Number of preunit's parents that we've not seen.
        /// </summary>
        public int MissingParents;

        /// <summary>
        /// Number of preunit's parents that are waiting in adder.
        /// </summary>
        public int WaitingParents;

        /// <summary>
        /// List of other preunits that has this preunit as parent.
        /// </summary>
        public List<WaitingPreunit> Children = null!;

        /// <summary>
        /// Flag for signaling problems with adding this unit.
        /// </summary>
        public bool Failed;
    }
}
