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
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Aurem.Syncing
{
    public class MulticastServerLegacy : IService
    {
        public const int OutPoolSize = 1;
        public const int InPoolSize = 1;

        protected ushort Pid;
        protected ushort NProc;
        protected IOrderer Orderer;
        protected Server Netserv;
        protected Channel<MCastRequest>[] Requests;
        protected IWorkerPool OutPool;
        protected IWorkerPool InPool;
        protected CancellationTokenSource StopOut;
        protected Logger Log;

        protected MulticastServerLegacy(Config.Config conf, IOrderer orderer, Server netserv, Logger log)
        {
            Requests = new Channel<MCastRequest>[conf.NProc];
            for (int i = 0; i < conf.NProc; i++)
            {
                Requests[i] = Channel.CreateBounded<MCastRequest>(conf.EpochLength);
            }

            Pid = conf.Pid;
            NProc = conf.NProc;
            Orderer = orderer;
            Netserv = netserv;
            StopOut = new CancellationTokenSource();
            Log = log;
            OutPool = new PerPidWorkerPool(conf.NProc, OutPoolSize, Out);
            InPool = new WorkerPool(InPoolSize * conf.NProc, In);
        }

        public static (IService, Requests.Multicast) NewServer(Config.Config conf, IOrderer orderer, Server netserv, Logger log)
        {
            var s = new MulticastServerLegacy(conf, orderer, netserv, log);
            return (s, s.Send);
        }

        public Task<Exception?> Start()
        {
            OutPool.Start();
            InPool.Start();
            return Task.FromResult<Exception?>(null);
        }

        public async Task StopAsync()
        {
            StopOut.Cancel();
            await OutPool.StopAsync();
            await InPool.StopAsync();
        }

        protected virtual async Task Send(IUnit u)
        {
            if (u.Creator() != Pid)
            {
                throw new Exception("Attempting to multicast unit that we didn't create");
            }

            var encUnit = DelegateExtensions.InvokeAndCaptureException(EncodeUtil.EncodeUnit, u, out var err);
            if (err != null)
            {
                Log.Error().Str("where", "MulticastServer.Send.EncodeUnit").Msg(err.Message);
                return;
            }


            var indices = ShuffleOrder(NProc);
            foreach (var idx in indices)
            {
                if (idx == Pid) continue;
                await Requests[idx].Writer.WriteAsync(new MCastRequest { EncodedUnit = encUnit, Height = u.Height() }); // unlikely to block for long, if at all
            }
        }

        public static int[] ShuffleOrder(ushort nproc, int seed = -1)
        {
            // Fisher-Yates shuffle
            var indices = Enumerable.Range(0, nproc).ToArray();
            var rng = (seed == -1) ? new System.Random() : new System.Random(seed);
            for (int i = 0; i <= indices.Length - 2; i++)
            {
                int j = i + rng.Next(indices.Length - i);
                (indices[j], indices[i]) = (indices[i], indices[j]);
            }

            return indices;
        }

        protected virtual async Task In()
        {
            (var conn, var err) = await DelegateExtensions.InvokeAndCaptureExceptionAsync(Netserv.Listen);
            if (err != null)
            {
                return;
            }

            try
            {
                (var preunit, err) = await DelegateExtensions.InvokeAndCaptureExceptionAsync(EncodeUtil.ReadPreunitAsync, conn);
                if (err != null)
                {
                    Log.Error().Str("where", "Multicast.In.Decode").Msg(err.Message);
                    return;
                }

                LoggingUtil.AddingErrors(await Orderer.AddPreunits(preunit.Creator(), preunit), 1, Log);
            }
            finally
            {
                await conn.Close();
            }
        }

        protected virtual async Task Out(ushort pid)
        {
            var r = await Requests[pid].Reader.ReadAsync(StopOut.Token);
            if (StopOut.Token.IsCancellationRequested)
            {
                return;
            }

            (var conn, var err) = await Netserv.TryDial(pid);
            if (err != null)
            {
                return;
            }

            try
            {
                (_, err) = await conn!.TryWrite(r.EncodedUnit);
                if (err != null)
                {
                    Log.Error().Str("where", "Multicast.Out.SendUnit").Msg(err.Message);
                    return;
                }

                err = conn!.TryFlush();
                if (err != null)
                {
                    Log.Error().Str("where", "Multicast.Out.Flush").Msg(err.Message);
                    return;
                }

                Log.Info().Val(Logging.Constants.Height, r.Height).Val(Logging.Constants.PID, pid).Msg(Logging.Constants.SentUnit);
            }
            finally
            {
                await conn!.Close();
            }
        }
    }
}
