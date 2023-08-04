using Aurem.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Creating
{
    /// <summary>
    /// Responsible for building and verifying epoch proofs.
    /// </summary>
    public interface IEpochProofBuilder
    {
        /// <summary>
        /// Checks if the given unit is a valid proof of epoch pu.EpochID() - 1.
        /// </summary>
        /// <param name="preunit"></param>
        /// <returns></returns>
        public bool Verify(IPreunit pu);

        /// <summary>
        /// Attempts to construct an epoch proof.
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        public byte[] TryBuilding(IUnit unit);

        /// <summary>
        /// Creates our share of the epoch proof.
        /// </summary>
        /// <param name="lastTimingUnit"></param>
        /// <returns></returns>
        public byte[] BuildShare(IUnit lastTimingUnit);
    }
}
