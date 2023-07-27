using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Model
{
    /// <summary>
    /// Orders units and preunits together.
    /// </summary>
    public interface IOrderer
    {
        /// <summary>
        /// Sends preunits to the orderer, received from another committee member.
        /// </summary>
        /// <param name="proc">The committee member the preunits were received from.</param>
        /// <param name="preunits">The preunits received.</param>
        /// <returns>A <see cref="List{T}"/> of <see cref="Exception"/> corresponding to potential errors with the received data.</returns>
        public List<Exception> AddPreunits(ushort proc, params IPreunit[] preunits);

        /// <summary>
        /// Finds units with the given IDs in the <see cref="IOrderer"/>. Returns null in the corresponding position if the requested unit isn't present. In the case of forks, returns all known units with a particular ID.
        /// </summary>
        /// <param name="ids">The IDs corresponding to the units to find. </param>
        /// <returns>A <see cref="List{T}"/> of <see cref="IUnit"/> corresponding to each id. </returns>
        public List<IUnit> UnitsByID(params ulong[] ids);

        /// <summary>
        /// Finds units with the given hashes in the <see cref="IOrderer"/>. Returns null in the corresponding position if the requested unit isn't present.
        /// </summary>
        /// <param name="hashes">The hashes corresponding to the units to find.</param>
        /// <returns>A <see cref="List{T}"/> of <see cref="IUnit"/> corresponding to each hash.</returns>
        public List<IUnit> UnitsByHash(params Hash[] hashes);

        /// <summary>
        /// Returns the maximal units per process for the given epoch. Returns null if the epoch is not known.
        /// </summary>
        /// <param name="epochId">The epoch ID corresponding to the epoch to find maximal units for.</param>
        /// <returns>An <see cref="ISlottedUnits"/> containing the maximal units for all known processes for the known given epoch.</returns>
        public ISlottedUnits MaxUnits(uint epochId);

        /// <summary>
        /// Returns DagInfo of the newest epoch.
        /// </summary>
        /// <returns></returns>
        public DagInfo[] GetInfo();

        /// <summary>
        /// Returns all the units present in the <see cref="IOrderer"/> that are above heights indicated by the provided DagInfo, including all units from newer epochs.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public List<IUnit> Delta(DagInfo[] info);

        /// <summary>
        /// Starts the orderer using the provided <see cref="IRandomSourceFactory"/>, <see cref="ISyncer"/>, and <see cref="IAlerter"/>.
        /// </summary>
        /// <param name="randomSourceFactory"></param>
        /// <param name="syncer"></param>
        /// <param name="alerter"></param>
        public void Start(IRandomSourceFactory randomSourceFactory, ISyncer syncer, IAlerter alerter);

        /// <summary>
        /// Stops the orderer.
        /// </summary>
        public void Stop();
    }
}
