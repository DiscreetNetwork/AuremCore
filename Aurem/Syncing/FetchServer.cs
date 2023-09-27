using Aurem.Model;
using Aurem.Serialize;
using AuremCore.Core;
using AuremCore.Core.Extensions;
using AuremCore.FastLogger;
using AuremCore.Network;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Aurem.Syncing
{
    public class FetchServer : IService
    {
        protected ushort Pid;
        protected IOrderer Orderer;
        protected Server Netserv;
        protected Channel<IDRequest> Requests;
        protected uint[] SyncIDs;
        protected IWorkerPool OutPool;
        protected IWorkerPool InPool;
        protected CancellationTokenSource StopOut;
        protected Logger Log;

        protected FetchServer(Config.Config conf, IOrderer orderer, Server netserv, Logger log)
        {
            Pid = conf.Pid;
            Orderer = orderer;
            Netserv = netserv;
            Requests = Channel.CreateBounded<IDRequest>(conf.NProc);
            SyncIDs = new uint[conf.NProc];
            StopOut = new CancellationTokenSource();
            Log = log;
            InPool = new WorkerPool(conf.FetchWorkers[0], In);
            OutPool = new WorkerPool(conf.FetchWorkers[1], Out);
        }

        public static (IService, Requests.Fetch) NewServer(Config.Config conf, IOrderer orderer, Server netserv, Logger log)
        {
            var s = new FetchServer(conf, orderer, netserv, log);
            return (s, s.Trigger);
        }

        public Exception? Start()
        {
            OutPool.Start();
            InPool.Start();
            return null;
        }

        public async Task StopAsync()
        {
            StopOut.Cancel();
            await OutPool.StopAsync();
            await InPool.StopAsync();
        }

        public async Task Trigger(ushort pid, ulong[] unitIDs)
        {
            var success = Requests.Writer.TryWrite(new IDRequest { Pid = pid, UnitIDs = unitIDs });
            if (!success)
            {
                Log.Warn().Msg(Logging.Constants.RequestOverload);
            }
        }

        public async Task In()
        {

            (var conn, var err) = await DelegateExtensions.InvokeAndCaptureExceptionAsync(Netserv.Listen);
            if (err != null)
            {
                return;
            }
            
            try
            {
                (var pid, var sid, err) = await Handshakes.AcceptGreeting(conn);
                if (err != null)
                {
                    Log.Error().Str("where", "FetchServer.In.Greeting").Msg(err.Message);
                    return;
                }

                if (pid >= (ushort)SyncIDs.Length)
                {
                    Log.Warn().Val(Logging.Constants.PID, pid).Msg("Called by a stranger");
                    return;
                }

                var log = Log.With().Val(Logging.Constants.PID, pid).Val(Logging.Constants.ISID, sid).Logger();
                log.Info().Msg(Logging.Constants.SyncStarted);

                (var unitIDs, err) = await IDRequest.ReceiveRequests(conn);
                if (err != null)
                {
                    log.Error().Str("where", "Fetch.In.ReceiveRequests").Msg(err.Message);
                    return;
                }

                IUnit[] units;
                try
                {
                    units = await Orderer.UnitsByID(unitIDs);
                }
                catch (Exception e)
                {
                    log.Error().Str("where", "Fetch.In.GetUnits").Msg(e.Message);
                    return;
                }

                log.Debug().Val(Logging.Constants.Sent, units.Length).Msg(Logging.Constants.SendUnits);

                try
                {
                    await EncodeUtil.WriteChunkAsync(units, conn);
                }
                catch (Exception e)
                {
                    log.Error().Str("where", "Fetch.In.SendUnits").Msg(e.Message);
                    return;
                }

                err = conn.TryFlush();
                if (err != null)
                {
                    log.Error().Str("where", "Fetch.In.Flush").Msg(err.Message);
                    return;
                }

                log.Info().Val(Logging.Constants.Sent, units.Length).Msg(Logging.Constants.SyncCompleted);
            }
            finally
            {
                await conn.Close();
            }
        }

        public async Task Out()
        {
            var r = await Requests.Reader.ReadAsync(StopOut.Token);
            if (StopOut.IsCancellationRequested) return;

            var remotePid = r.Pid;
            (var conn, var err) = await Netserv.TryDial(remotePid);
            if (err != null)
            {
                return;
            }

            try
            {
                var sid = SyncIDs[remotePid];
                SyncIDs[remotePid]++;
                var log = Log.With().Val(Logging.Constants.PID, remotePid).Val(Logging.Constants.OSID, sid).Logger();
                log.Info().Msg(Logging.Constants.SyncStarted);

                err = await Handshakes.Greet(conn!, Pid, sid);
                if (err != null)
                {
                    log.Error().Str("where", "Fetch.Out.Greeting").Msg(err.Message);
                    return;
                }

                err = await IDRequest.SendRequests(conn!, r.UnitIDs);
                if (err != null)
                {
                    log.Error().Str("where", "Fetch.Out.SendRequests").Msg(err.Message);
                    return;
                }

                log.Debug().Msg(Logging.Constants.GetUnits);

                IPreunit[] units;
                try
                {
                    units = await EncodeUtil.ReadChunkAsync(conn!);
                }
                catch (Exception e)
                {
                    log.Error().Str("where", "Fetch.Out.ReceivePreunits").Msg(e.Message);
                    return;
                }

                var errs = await Orderer.AddPreunits(remotePid, units);
                Logging.LoggingUtil.AddingErrors(errs, units.Length, log);
                log.Info().Val(Logging.Constants.Recv, units.Length).Msg(Logging.Constants.SyncCompleted);
            }
            finally
            {
                await conn!.Close();
            }
        }
    }
}
