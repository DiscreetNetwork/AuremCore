using Aurem.Logging;
using Aurem.Model;
using Aurem.Model.Exceptions;
using Aurem.Serialize;
using AuremCore.Core;
using AuremCore.Core.Extensions;
using AuremCore.FastLogger;
using AuremCore.Network;
using AuremCore.RMC;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Syncing
{
    public class RmcServerLegacy : IService
    {
        public const int InPoolSize = 1;
        public const string RmcMismatch = "unit differs from successfully RMC'd unit";
        public const byte MsgSendData = 0;
        public const byte MsgSendProof = 1;
        public const byte MsgRequestFinished = 2;

        protected ushort Pid;
        protected ushort NProc;
        protected IOrderer Orderer;
        protected Server Netserv;
        protected ReliableMulticast State;
        protected IWorkerPool InPool;
        protected Logger Log;

        protected SemaphoreSlim multicastInProgress;
        protected bool Quit;
        protected AsyncReaderWriterLock Mx;
        protected WaitGroup Wg;

        protected RmcServerLegacy(Config.Config conf, IOrderer orderer, Server netserv, Logger log)
        {
            NProc = conf.NProc;
            Pid = conf.Pid;
            Orderer = orderer;
            Netserv = netserv;
            State = new ReliableMulticast(conf.RMCPublicKeys, conf.RMCPrivateKey);
            Log = log;
            InPool = new WorkerPool(InPoolSize * conf.NProc, In);
            conf.AddCheck((u, d) => FinishedRMC(u, d).GetAwaiter().GetResult());
            
            multicastInProgress = new SemaphoreSlim(1, 1);
            Quit = false;
            Mx = new AsyncReaderWriterLock();
            Wg = new WaitGroup();
        }

        public static (IService, Requests.Multicast) NewServer(Config.Config conf, IOrderer orderer, Server netserv, Logger log)
        {
            var s = new RmcServerLegacy(conf, orderer, netserv, log);
            return (s, s.Send);
        }

        public Task<Exception?> Start()
        {
            InPool.Start();
            return Task.FromResult<Exception?>(null);
        }

        public void StopIn()
        {

        }

        public async Task StopAsync()
        {
            var _lock = await Mx.WriterLockAsync();

            try
            {
                Quit = true;
                await Wg.WaitAsync();
                InPool.Stop();
            }
            finally
            {
                _lock.Dispose();
            }
        }

        public async Task Send(IUnit u)
        {
            var rlock = await Mx.ReaderLockAsync();

            try
            {
                if (Quit) return;

                if (u.Creator() == Pid)
                {
                    Wg.Add(1);
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Multicast(u);
                        }
                        finally
                        {
                            Wg.Done();
                        }
                    });
                }
            }
            finally
            {
                rlock.Dispose();
            }
        }

        public async Task<Exception?> FinishedRMC(IUnit u, IDag d)
        {
            if (u.Creator() == Pid) return null;

            var rmcID = u.UnitID();
            IPreunit pu;
            Exception? err;
            if (State.Status(rmcID) != Status.Finished)
            {
                (pu, err) = await FetchFinishedFromAll(u);
            }
            else
            {
                pu = DelegateExtensions.InvokeAndCaptureException(EncodeUtil.DecodeUnit, State.Data(rmcID), out err);
            }

            if (err != null)
            {
                return err;
            }

            if (!u.PEquals(pu))
            {
                return new ComplianceException(RmcMismatch);
            }

            return null;
        }

        public long HashToInt64(Hash h)
        {
            long result = 0;
            for (int p = 0; p < h.Length; p++)
            {
                result += h.Data[p] * (1L << p);
            }

            return result;
        }

        public int HashToInt32(Hash h)
        {
            var result = HashToInt64(h);
            return (int)(result >> 32) + ((int)result & 0x7FFFFFFF);
        }

        public async Task<(IPreunit, Exception?)> FetchFinishedFromAll(IUnit u)
        {
            (var pu, var err) = await FetchFinished(u, u.Creator());
            if (err != null)
            {
                Log.Error().Str("where", "Rmc.FetchFinishedFromAll.CallForPid").Msg(err.Message);
            }

            // call all other nodes in random order
            var indices = MulticastServer.ShuffleOrder(NProc, HashToInt32(u.Hash()));
            foreach (var pidi in indices)
            {
                if (pu != null) break;

                var pid = (ushort)pidi;
                if (pid == Pid || pid == u.Creator()) continue;

                (pu, err) = await FetchFinished(u, pid);
                if (err != null)
                {
                    Log.Error().Str("where", "Rmc.FetchFinishedFromAll.CallForPid").Msg(err.Message);
                    continue;
                }
            }

            if (pu == null)
            {
                return (null!, new Exception($"Rmc.FetchFinishedFromAll: unable to fetch a finished unit (creator={u.Creator()}, height={u.Height()}, hash={u.Hash()}"));
            }

            return (pu, null);
        }

        public async Task<(IPreunit, Exception?)> FetchFinished(IUnit u, ushort pid)
        {
            (var conn, var err) = await Netserv.TryDial(pid);
            if (err != null)
            {
                return (null!, new Exception($"Rmc.FetcFinishedFromAll.Dial for PID={pid}: {err.Message}"));
            }

            try
            {
                var id = u.UnitID();
                err = await DelegateExtensions.InvokeAndCaptureExceptionAsync(Greetings.Greet, conn!, Pid, id, MsgRequestFinished);
                if (err != null)
                {
                    return (null!, new Exception($"Rmc.FetchFinished.Greet for PID={pid}: {err.Message}"));
                }

                err = conn!.TryFlush();
                if (err != null)
                {
                    return (null!, new Exception($"Rmc.FetchFinished.Flush for PID={pid}: {err.Message}"));
                }

                (var data, err) = await State.AcceptFinished(id, u.Creator(), conn!);
                if (err != null)
                {
                    return (null!, new Exception($"Rmc.FetchFinished.AcceptFinished for PID={pid}: {err.Message}"));
                }

                var pu = DelegateExtensions.InvokeAndCaptureException(EncodeUtil.DecodeUnit, data, out err);
                if (err != null)
                {
                    return (null!, new Exception($"Rmc.FetchFinished.DecodeUnit for PID={pid}: {err.Message}"));
                }

                return (pu, null);
            }
            finally
            {
                err = await DelegateExtensions.InvokeAndCaptureExceptionAsync(conn!.Close);
                if (err != null)
                {
                    Log.Error().Str("where", "Rmc.FetchFinishedFromAll.Close").Msg($"error while closing connection for PID={pid}: {err.Message}");
                }
            }
        }

        public async Task SendFinished(ulong id, Conn conn, Logger log)
        {
            if (State.Status(id) != Status.Finished)
            {
                log.Error().Str("where", "Rmc.In.SendFinished").Msg("requested to send finished before RMC reached the finished state");
                return;
            }

            var err = await State.SendFinished(id, conn);
            if (err != null)
            {
                log.Error().Str("where", "Rmc.In.SendFinished").Msg(err.Message);
                return;
            }

            err = conn.TryFlush();
            if (err != null)
            {
                log.Error().Str("where", "Rmc.In.Flush").Msg(err.Message);
            }
        }

        public async Task AcceptData(ulong id, ushort sender, Conn conn, Logger log)
        {
            (_, var creator, _) = IPreunit.DecodeID(id);
            if (creator != sender)
            {
                log.Error().Str("where", "Rmc.In.AcceptData").Msg("pid and id mismatch");
                return;
            }

            (var data, var err) = await State.AcceptData(id, sender, conn);
            if (err != null)
            {
                log.Error().Str("where", "Rmc.In.AcceptData").Msg(err.Message);
                return;
            }

            var pu = DelegateExtensions.InvokeAndCaptureException(EncodeUtil.DecodeUnit, data, out err);
            if (err != null)
            {
                log.Error().Str("where", "Rmc.In.DecodeUnit").Msg(err.Message);
                return;
            }

            if (id != pu.UnitID())
            {
                log.Error().Str("what", "wrong preunit id").Msg($"wrong preunit id; expected {id} but got {pu.UnitID()}");
                return;
            }

            err = await State.SendSignature(id, conn);
            if (err != null)
            {
                log.Error().Str("where", "Rmc.In.SendSignature").Msg(err.Message);
                return;
            }

            err = conn.TryFlush();
            if (err != null)
            {
                log.Error().Str("where", "Rmc.In.Flush").Msg(err.Message);
            }
        }

        public async Task<bool> AcceptProof(ulong id, Conn conn, Logger log)
        {
            var err = await State.AcceptProof(id, conn);
            if (err != null)
            {
                log.Error().Str("where", "Rmc.AcceptProof.AcceptProof").Msg(err.Message);
                return false;
            }

            return true;
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
                ((var pid, var id, var msgType), err) = await DelegateExtensions.InvokeAndCaptureExceptionAsync(Greetings.AcceptGreeting, conn);
                if (err != null)
                {
                    Log.Error().Str("where", "Rmc.In.AcceptGreeting").Msg(err.Message);
                    return;
                }

                var log = Log.With().Val(Logging.Constants.PID, pid).Val(Logging.Constants.ISID, id).Logger();
                log.Info().Msg(Logging.Constants.SyncStarted);

                switch (msgType)
                {
                    case MsgSendData:
                        await AcceptData(id, pid, conn, log);
                        break;
                    case MsgSendProof:
                        if (await AcceptProof(id, conn, log))
                        {
                            var pu = DelegateExtensions.InvokeAndCaptureException(EncodeUtil.DecodeUnit, State.Data(id), out err);
                            if (err != null)
                            {
                                log.Error().Str("where", "Rmc.In.DecodeUnit").Msg(err.Message);
                                return;
                            }
                            LoggingUtil.AddingErrors(await Orderer.AddPreunits(pu.Creator(), pu), 1, log);
                        }
                        break;
                    case MsgRequestFinished:
                        await SendFinished(id, conn, log);
                        break;
                    default:
                        log.Error().Str("where", "Rmc.In.Switch").Msg($"unknown message type {msgType}");
                        return;
                }

                log.Info().Msg(Logging.Constants.SyncCompleted);
            }
            finally
            {
                await conn.Close();
            }
        }

        public async Task<Exception?> AttemptGather(Conn conn, byte[] data, ulong id, ushort recipient)
        {
            try
            {
                var err = await DelegateExtensions.InvokeAndCaptureExceptionAsync(Greetings.Greet, conn, Pid, id, MsgSendData);
                if (err != null) return err;

                err = await State.SendData(id, data, conn);
                if (err != null) return err;

                err = conn.TryFlush();
                if (err != null) return err;

                (_, err) = await State.AcceptSignature(id, recipient, conn);
                if (err != null) return err;

                return null;
            }
            finally
            {
                await conn.Close();
            }
        }

        public async Task<bool> GetMemberSignature(byte[] data, ulong id, ushort recipient)
        {
            var log = Log.With().Val(Logging.Constants.PID, recipient).Val(Logging.Constants.OSID, id).Logger();
            while (State.Status(id) != Status.Finished)
            {
                (var conn, var err) = await Netserv.TryDial(recipient);
                if (err != null)
                {
                    log.Error().Str("where", "Rmc.GetMemberSignature.Dial").Msg(err.Message);
                    await Task.Delay(50);
                    continue;
                }

                log.Info().Msg(Logging.Constants.SyncStarted);
                err = await AttemptGather(conn!, data, id, recipient);
                if (err != null)
                {
                    log.Error().Str("where", "Rmc.AttemptGather").Msg(err.Message);
                    await Task.Delay(50);
                    continue;
                }

                log.Info().Msg(Logging.Constants.SyncCompleted);
                return true;
            }

            return false;
        }

        public async Task<bool[]> GetCommitteeSignatures(byte[] data, ulong id)
        {
            var signedBy = new bool[NProc];
            var gathering = new WaitGroup();
            for (ushort pid = 0; pid < NProc; pid++)
            {
                if (pid == Pid) continue;
                var _pid = pid;
                gathering.Add(1);
                _ = Task.Run(async () =>
                {
                    try
                    {
                        signedBy[_pid] = await GetMemberSignature(data, id, _pid);
                    }
                    finally
                    {
                        gathering.Done();
                    }
                });
            }

            await gathering.WaitAsync();
            return signedBy;
        }

        public async Task<Exception?> SendProof(ushort recipient, ulong id)
        {
            (var conn, var err) = await Netserv.TryDial(recipient);
            if (err != null) return err;

            err = await DelegateExtensions.InvokeAndCaptureExceptionAsync(Greetings.Greet, conn!, Pid, id, MsgSendProof);
            if (err != null) return err;

            err = await State.SendProof(id, conn!);
            if (err != null) return err;

            err = conn!.TryFlush();
            if (err != null) return err;

            return null;
        }

        public async Task Multicast(IUnit u)
        {
            var id = u.UnitID();
            var data = DelegateExtensions.InvokeAndCaptureException(EncodeUtil.EncodeUnit, u, out var err);

            if (err != null)
            {
                Log.Error().Str("where", "RmcServer.Send.EncodeUnit").Msg(err.Message);
                return;
            }

            await multicastInProgress.WaitAsync();
            await GetCommitteeSignatures(data, id);
            multicastInProgress.Release();

            var wg = new WaitGroup();
            for (ushort pid = 0; pid < NProc; pid++)
            {
                if (pid == Pid) continue;
                var _pid = pid;
                wg.Add(1);
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var err = await SendProof(_pid, id);
                        if (err != null)
                        {
                            Log.Error().Str("where", "RmcServer.SendProof").Msg(err.Message);
                        }
                    }
                    finally
                    {
                        wg.Done();
                    }
                });
            }

            await wg.WaitAsync();
        }
    }
}
