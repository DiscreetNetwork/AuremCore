using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.RMC
{
    /// <summary>
    /// Represents the state of a reliable multicast <see cref="ReliableMulticast"/>.
    /// </summary>
    public enum Status : byte
    {
        /// <summary>
        /// Means we either never saw any data related to an RMC instance, or we deleted it.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Means we received or sent data that is being multicast, but we haven't signed it yet.
        /// </summary>
        Data = 1,

        /// <summary>
        /// Means we signed the data, but we haven't yet received the proof that it has been multicast successfully.
        /// </summary>
        Signed = 2,

        /// <summary>
        /// Means we received a proof that the data has been multicast successfully.
        /// </summary>
        Finished = 3,
    }
}
