using Aurem.Logging;
using Aurem.Model;
using Aurem.Serialize;
using AuremCore.Core;
using AuremCore.Core.Extensions;
using AuremCore.FastLogger;
using AuremCore.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Aurem.Syncing
{
    public class GossipServer : IService
    {
        protected ushort NProc;
        protected ushort Pid;
        protected IOrderer Orderer;
        protected Server Netserv;
        protected Channel<ushort> Requests;
        protected uint[] SyncIDs;
        protected SemaphoreSlim[] Tokens;
        protected IWorkerPool OutPool;
        protected IWorkerPool InPool;
        protected CancellationTokenSource StopOut;
        protected Logger Log;

        protected GossipServer(Config.Config conf, IOrderer orderer, Server netserv, Logger log)
        {
            NProc = conf.NProc;
            Pid = conf.Pid;
            Orderer = orderer;
            Netserv = netserv;
            Requests = Channel.CreateBounded<ushort>(conf.NProc);
            SyncIDs = new uint[conf.NProc];
            Tokens = new SemaphoreSlim[conf.NProc];
            StopOut = new CancellationTokenSource();
            Log = log;

            for (int i = 0; i < Tokens.Length; i++)
            {
                Tokens[i] = new SemaphoreSlim(1, 1);
            }

            InPool = new WorkerPool(conf.GossipWorkers[0], In);
            OutPool = new WorkerPool(conf.GossipWorkers[1], Out);
        }

        public static (IService, Requests.Gossip) NewServer(Config.Config conf, IOrderer orderer, Server netserv, Logger log)
        {
            var gossip = new GossipServer(conf, orderer, netserv, log);
            return (gossip, gossip.Request);
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

        public async Task Request(ushort pid)
        {
            var success = Requests.Writer.TryWrite(pid);
            if (!success)
            {
                Log.Warn().Msg(Logging.Constants.RequestOverload);
            }
        }

        public async Task In()
        {
            (var conn, var err) = await DelegateExtensions.InvokeAndCaptureExceptionAsync(Netserv.Listen);
            if (err != null) return;

            try
            {
                // accept handshake
                (var pid, var sid, err) = await Handshakes.AcceptGreeting(conn);
                if (err != null)
                {
                    Log.Error().Str("where", "Gossip.In.Greeting").Msg(err.Message);
                    return;
                }

                if (pid >= NProc)
                {
                    Log.Warn().Val(Logging.Constants.PID, pid).Msg("Called by a stranger");
                    return;
                }

                if (Tokens[pid].CurrentCount == 0)
                {
                    return;
                }

                await Tokens[pid].WaitAsync();

                try
                {
                    var log = Log.With().Val(Logging.Constants.PID, pid).Val(Logging.Constants.ISID, sid).Logger();
                    log.Info().Msg(Logging.Constants.SyncStarted);

                    // 1. Receive their DagInfo
                    (var theirDagInfo, err) = await DelegateExtensions.InvokeAndCaptureExceptionAsync(EncodeUtil.ReadDagInfosAsync, conn);
                    if (err != null)
                    {
                        log.Error().Str("where", "Gossip.In.GetDagInfo").Msg(err.Message);
                        return;
                    }

                    // 2. Compute our DagInfo
                    var dagInfo = await Orderer.GetInfo();

                    // 3. Send our DagInfo
                    log.Debug().Msg(Logging.Constants.SendInfo);
                    err = await DelegateExtensions.InvokeAndCaptureExceptionAsync(EncodeUtil.WriteDagInfosAsync, dagInfo, conn);
                    if (err != null)
                    {
                        log.Error().Str("where", "Gossip.In.SendDagInfo").Msg(err.Message);
                        return;
                    }

                    // 4. Send units
                    var units = await Orderer.Delta(theirDagInfo);
                    log.Debug().Val(Logging.Constants.Sent, units.Count).Msg(Logging.Constants.SendUnits);
                    err = await DelegateExtensions.InvokeAndCaptureExceptionAsync(EncodeUtil.WriteChunkAsync, units, conn);
                    if (err != null)
                    {
                        log.Error().Str("where", "Gossip.In.SendUnits").Msg(err.Message);
                        return;
                    }

                    err = conn.TryFlush();
                    if (err != null)
                    {
                        log.Error().Str("where", "Gossip.In.Flush").Msg(err.Message);
                        return;
                    }

                    // 5. Receive units
                    log.Debug().Msg(Logging.Constants.GetUnits);
                    (var theirPreunitsReceived, err) = await DelegateExtensions.InvokeAndCaptureExceptionAsync(EncodeUtil.ReadChunkAsync, conn);
                    if (err != null)
                    {
                        log.Error().Str("where", "Gossip.In.GetPreunits").Msg(err.Message);
                        return;
                    }

                    // 6. Add units
                    var errs = await Orderer.AddPreunits(pid, theirPreunitsReceived);
                    LoggingUtil.AddingErrors(errs, theirPreunitsReceived.Length, log);
                    log.Info().Val(Logging.Constants.Recv, theirPreunitsReceived.Length).Val(Logging.Constants.Sent, units.Count).Msg(Logging.Constants.SyncCompleted);
                }
                finally
                {
                    Tokens[pid].Release();
                }
            }
            finally
            {
                await conn.Close();
            }
        }

        public async Task Out()
        {
            ushort remotePid = await Requests.Reader.ReadAsync(StopOut.Token);
            if (StopOut.IsCancellationRequested)
            {
                return;
            }

            if (Tokens[remotePid].CurrentCount == 0)
            {
                return;
            }

            await Tokens[remotePid].WaitAsync();

            try
            {
                (var _conn, var err) = await Netserv.TryDial(remotePid);
                if (err != null)
                {
                    return;
                }
                Conn conn = _conn!;

                try
                {
                    var sid = SyncIDs[remotePid];
                    SyncIDs[remotePid]++;

                    var log = Log.With().Val(Logging.Constants.PID, remotePid).Val(Logging.Constants.OSID, sid).Logger();
                    log.Info().Msg(Logging.Constants.SyncStarted);

                    // handshake
                    err = await Handshakes.Greet(conn!, Pid, sid);
                    if (err != null)
                    {
                        log.Error().Str("where", "Gossip.Out.Greeting").Msg(err.Message);
                        return;
                    }

                    // 2. Send DagInfo
                    var dagInfo = await Orderer.GetInfo();
                    log.Debug().Msg(Logging.Constants.SendInfo);
                    err = await DelegateExtensions.InvokeAndCaptureExceptionAsync(EncodeUtil.WriteDagInfosAsync, dagInfo, conn);
                    if (err != null)
                    {
                        log.Error().Str("where", "Gossip.Out.SendDagInfo").Msg(err.Message);
                        return;
                    }

                    err = conn.TryFlush();
                    if (err != null)
                    {
                        log.Error().Str("where", "Gossip.Out.Flush").Msg(err.Message);
                        return;
                    }

                    // 3. Receive DagInfo
                    log.Debug().Msg(Logging.Constants.GetInfo);
                    (var theirDagInfo, err) = await DelegateExtensions.InvokeAndCaptureExceptionAsync(EncodeUtil.ReadDagInfosAsync, conn);
                    if (err != null)
                    {
                        // errors may happen here when the remote peer rejects the gossip attempt, thus aren't true errors.
                        log.Debug().Str("where", "Gossip.Out.GetDagInfo").Msg(err.Message);
                        return;
                    }

                    // 4. Receive units
                    log.Debug().Msg(Logging.Constants.GetUnits);
                    (var theirPreunitsReceived, err) = await DelegateExtensions.InvokeAndCaptureExceptionAsync(EncodeUtil.ReadChunkAsync, conn);
                    if (err != null)
                    {
                        log.Error().Str("where", "Gossip.Out.GetPreunits").Msg(err.Message);
                        return;
                    }

                    // 5. Send units
                    var units = await Orderer.Delta(theirDagInfo);
                    log.Debug().Val(Logging.Constants.Sent, units.Count).Msg(Logging.Constants.SendUnits);
                    err = await DelegateExtensions.InvokeAndCaptureExceptionAsync(EncodeUtil.WriteChunkAsync, units, conn);
                    if (err != null)
                    {
                        log.Error().Str("where", "Gossip.Out.SendUnits").Msg(err.Message);
                        return;
                    }

                    err = conn.TryFlush();
                    if (err != null)
                    {
                        log.Error().Str("where", "Gossip.Out.Flush`2").Msg(err.Message);
                        return;
                    }

                    // 6. Add units to dag
                    var errs = await Orderer.AddPreunits(remotePid, theirPreunitsReceived);
                    LoggingUtil.AddingErrors(errs, theirPreunitsReceived.Length, log);
                    log.Info().Val(Logging.Constants.Recv, theirPreunitsReceived.Length).Val(Logging.Constants.Sent, units.Count).Msg(Logging.Constants.SyncCompleted);
                }
                finally
                {
                    await conn!.Close();
                }
            }
            finally
            {
                Tokens[remotePid].Release();
            }
        }
    }
}
