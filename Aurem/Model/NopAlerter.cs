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
    /// NopAlerter is an <see cref="IAlerter"/> that does nothing.
    /// </summary>
    public class NopAlerter : IAlerter
    {
        public void Start() { }

        public void Stop() { }

        public void NewFork(IPreunit a, IPreunit b) { }

        public void HandleIncoming(Conn conn) { }

        public IUnit Disambiguate(IUnit[] units, IPreunit preunit) { return null; }

        public void RequestCommitment(IPreunit preunit, ushort proc) { }

        public void ResolveMissingCommitment(Exception exc, IPreunit preunit, ushort proc) { }

        public bool IsForker(ushort proc) { return false; }

        public IObserverManager AddForkObserver(Action<IPreunit, IPreunit> handler) { return new NopObserverManager(); }

        public void Lock(ushort proc) { }

        public void Unlock(ushort proc) { }
    }

    public class NopObserverManager : IObserverManager
    {
        public void RemoveObserver() { }
    }
}
