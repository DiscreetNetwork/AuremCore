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
        public static readonly NopAlerter Instance = new NopAlerter();

        public void Start() { }

        public Task Stop() => Task.CompletedTask;

        public Task NewFork(IPreunit a, IPreunit b) => Task.CompletedTask;

        public Task HandleIncoming(Conn conn) => Task.CompletedTask;

        public Task<(IUnit, Exception?)> Disambiguate(IUnit[] units, IPreunit preunit) => Task.FromResult<(IUnit, Exception?)>((null, null));

        public Task<Exception?> RequestCommitment(IPreunit preunit, ushort proc) => Task.FromResult<Exception?>(null);

        public Task<Exception?> ResolveMissingCommitment(Exception exc, IPreunit preunit, ushort proc) => Task.FromResult<Exception?>(null);

        public bool IsForker(ushort proc) { return false; }

        public IObserverManager AddForkObserver(Action<IPreunit, IPreunit> handler) { return new NopObserverManager(); }

        public Task Lock(ushort proc) => Task.CompletedTask;

        public void Unlock(ushort proc) { }
    }

    public class NopObserverManager : IObserverManager
    {
        public void RemoveObserver() { }
    }
}
