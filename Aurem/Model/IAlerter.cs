using AuremCore.Core;
using AuremCore.Network;
using AuremCore.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Model
{
    /// <summary>
    /// Alerter is responsible for raising alerts about forks and handling communication about commitments in the case of forks.
    /// </summary>
    public interface IAlerter
    {
        /// <summary>
        /// Raises an alert about a newly detected fork.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public Task NewFork(IPreunit a, IPreunit b);

        /// <summary>
        /// Handles an incoming connection.
        /// </summary>
        /// <param name="conn"></param>
        public Task HandleIncoming(Conn conn);

        /// <summary>
        /// Disambiguates between which of the provided (forked) units is the right one to be the parent of the given preunit.
        /// </summary>
        /// <param name="units"></param>
        /// <param name="preunit"></param>
        /// <returns></returns>
        public (IUnit, Exception?) Disambiguate(IUnit[] units, IPreunit preunit);

        /// <summary>
        /// Requests a commitment that is missing in the given preunit from the committee member with the given process ID.
        /// </summary>
        /// <param name="preunit"></param>
        /// <param name="proc"></param>
        public Task<Exception?> RequestCommitment(IPreunit preunit, ushort proc);

        /// <summary>
        /// Attempts to resolve the missing commitment.
        /// </summary>
        /// <param name="exc"></param>
        /// <param name="preunit"></param>
        /// <param name="proc"></param>
        public Task<Exception?> ResolveMissingCommitment(Exception exc, IPreunit preunit, ushort proc);

        /// <summary>
        /// Checks whether the alerter knows that the given pid is a forker.
        /// </summary>
        /// <param name="proc"></param>
        /// <returns></returns>
        public bool IsForker(ushort proc);

        /// <summary>
        /// Allows one to receive notifications in case a fork is discovered.
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public IObserverManager AddForkObserver(Action<IPreunit, IPreunit> handler);

        /// <summary>
        /// Locks the state for a given process ID.
        /// </summary>
        /// <param name="proc"></param>
        public Task Lock(ushort proc);

        /// <summary>
        /// Unlocks the state for a given process ID.
        /// </summary>
        /// <param name="proc"></param>
        public void Unlock(ushort proc);

        /// <summary>
        /// Starts the Alerter.
        /// </summary>
        public void Start();

        /// <summary>
        /// Stops the Alerter.
        /// </summary>
        public Task Stop();
    }
}
