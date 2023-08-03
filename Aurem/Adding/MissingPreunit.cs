using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Adding
{
    public class MissingPreunit
    {
        /// <summary>
        /// List of WaitingPreunits that has this preunit as a parent.
        /// </summary>
        public List<WaitingPreunit> NeededBy;

        public DateTime Requested;

        public MissingPreunit()
        {
            NeededBy = new(8);
        }

        /// <summary>
        /// Adds another WaitingPreunit that needs this MissingPreunit.
        /// </summary>
        /// <param name="waitingPreunit"></param>
        public void AddNeeding(WaitingPreunit waitingPreunit)
        {
            lock (NeededBy) NeededBy.Add(waitingPreunit);
        }
    }
}
