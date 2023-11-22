using Aurem.Model;
using AuremCore.Core;
using AuremCore.FastLogger;
using AuremCore.Network;
using AuremCore.RMC;
using AuremCore.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Forking
{
    public class AlertService : IAlerter
    {
        public AlertHandler Handler;

        public IObserverManager AddForkObserver(Action<IPreunit, IPreunit> handler) => Handler.AddForkObserver(handler);

        public Task<(IUnit, Exception?)> Disambiguate(IUnit[] units, IPreunit preunit) => Handler.Disambiguate(units, preunit);

        public Task HandleIncoming(Conn conn) => Handler.HandleIncoming(conn);

        public bool IsForker(ushort proc) => Handler.IsForker(proc);

        public Task Lock(ushort proc) => Handler.Lock(proc);

        public Task NewFork(IPreunit a, IPreunit b) => Handler.NewFork(a, b);

        public Task<Exception?> RequestCommitment(IPreunit preunit, ushort proc) => Handler.RequestCommitment(preunit, proc);

        public Task<Exception?> ResolveMissingCommitment(Exception exc, IPreunit preunit, ushort proc) => Handler.ResolveMissingCommitment(exc, preunit, proc);

        public void Unlock(ushort proc) => Handler.Unlock(proc);

        public Server Netserv;
        public WaitGroup Listens;
        public ulong Quit;
        public Logger Log;

        public AlertService(Config.Config conf, IOrderer orderer, Server netserv, Logger log)
        {
            var rmc = new ReliableMulticast(conf.RMCPublicKeys, conf.RMCPrivateKey);
            Handler = new AlertHandler(conf, orderer, rmc, netserv, log);
            Netserv = netserv;
            Log = log.With().Val(Logging.Constants.Service, Logging.Constants.AlertService).Logger();
            Quit = 0;
            Listens = new WaitGroup();
        }

        public void Start()
        {
            Listens.Add(1);
            _ = Task.Run(async () => await HandleConns());
            Log.Log().Msg(Logging.Constants.ServiceStarted);
        }

        public async Task Stop()
        {
            Interlocked.Exchange(ref Quit, 1);
            await Listens.WaitAsync();
            Log.Log().Msg(Logging.Constants.ServiceStopped);
        }

        public async Task HandleConns()
        {
            try
            {
                while (Interlocked.Read(ref Quit) == 0)
                {
                    Conn conn;
                    try
                    {
                        conn = await Netserv.Listen();
                    }
                    catch
                    {
                        continue;
                    }

                    Listens.Add(1);
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Handler.HandleIncoming(conn);
                        }
                        finally
                        {
                            Listens.Done();
                        }
                    });
                }
            }
            finally
            {
                Listens.Done();
            }
        }
    }
}
