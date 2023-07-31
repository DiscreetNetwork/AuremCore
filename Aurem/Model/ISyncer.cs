﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Model
{
    /// <summary>
    /// Syncs data.
    /// </summary>
    public interface ISyncer
    {
        /// <summary>
        /// Request gossip with the given committee member.
        /// </summary>
        /// <param name="proc">The pid corresponding to the committee member to request gossip with.</param>
        public void RequestGossip(ushort proc);

        /// <summary>
        /// Sends a request to the given committee member for units with the given IDs.
        /// </summary>
        /// <param name="proc">The pid corresponding to the committee member to fetch units from.</param>
        /// <param name="uids">The unit IDs corresponding to the units to fetch.</param>
        public void RequestFetch(ushort proc, IEnumerable<ulong> uids);

        /// <summary>
        /// Multicasts a unit.
        /// </summary>
        /// <param name="unit">The unit to multicast.</param>
        public void Multicast(IUnit unit);

        /// <summary>
        /// Starts the syncer.
        /// </summary>
        public void Start();

        /// <summary>
        /// Stops the syncer.
        /// </summary>
        public void Stop();
    }
}