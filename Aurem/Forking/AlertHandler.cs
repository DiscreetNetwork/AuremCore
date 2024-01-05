using System;
using System.Collections.Generic;
using Aurem.Config;
using Aurem.Model;
using Aurem.Model.Exceptions;
using Aurem.Serialize;
using AuremCore.Core;
using AuremCore.FastLogger;
using AuremCore.Network;
using AuremCore.RMC;
using AuremCore.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aurem.Syncing.Internals;
using System.Collections.Concurrent;
using Aurem.Syncing;
using Aurem.Syncing.Internals.Packets.Bodies;
using AuremCore.Crypto.Multi;
using Aurem.Syncing.Internals.Packets;
using static Aurem.Syncing.RmcServer;
using System.Security.Cryptography;
using Aurem.Random;
using System.Diagnostics.CodeAnalysis;
using System.Buffers.Binary;

namespace Aurem.Forking
{
    public class AlertHandler
    {
        public struct ReqKey
        {
            public Hash Hash;
            public ushort Pid;

            public ReqKey(Hash hash, ushort pid)
            {
                Hash = hash;
                Pid = pid;
            }

            public override bool Equals([NotNullWhen(true)] object? obj)
            {
                if (obj is ReqKey other)
                {
                    return other.Pid == Pid && other.Hash == Hash;
                }
                else
                {
                    return false;
                }
            }

            public override int GetHashCode()
            {
                return (int)(((Pid << 16) | (~Pid)) ^ (BinaryPrimitives.ReadUInt32LittleEndian(Hash.Data[0..4])));
            }
        }

        public class ReqKeyEqualityComparer : IEqualityComparer<ReqKey>
        {
            public bool Equals(ReqKey x, ReqKey y) => x.Equals(y);

            public int GetHashCode([DisallowNull] ReqKey obj) => obj.GetHashCode();
        }

        public ushort MyPid;
        public ushort NProc;
        public int Session;
        public IOrderer Orderer;
        public IPublicKey[] Keys;
        //public ReliableMulticast Rmc;
        public Network Network;
        public CommitBase Commitments;
        public AuremCore.Utils.IObservable<ForkData> Observable; // FIXME: what type is our observable?

        private Logger Log;
        public readonly SemaphoreSlim[] locks;

        protected ConcurrentDictionary<ulong, RmcServer.RmcInstance> Out;
        protected ConcurrentDictionary<ulong, RmcServer.RmcIncoming> In;
        protected ConcurrentDictionary<ReqKey, TaskCompletionSource<AlertRequestCommitment>> Requests;
        protected ConcurrentDictionary<RmcServer.SigKey, TaskCompletionSource<RmcSignature>> MemberSigTasks;
        protected Keychain Kchain;
        
        internal bool Sessioned;

        public AlertHandler(Config.Config conf, IOrderer orderer, ReliableMulticast rmc, Network netserv, Logger log)
        {
            MyPid = conf.Pid;
            NProc = conf.NProc;
            Session = conf.Session;
            Sessioned = conf.Sessioned;
            Keys = conf.PublicKeys.ToArray();
            Orderer = orderer;
            Out = new();
            In = new();
            Requests = new(new ReqKeyEqualityComparer());
            MemberSigTasks = new(new RmcServer.SigKeyEqualityComparer());
            Kchain = new(conf.RMCPublicKeys, conf.RMCPrivateKey);
            Network = netserv;
            Commitments = new CommitBase();
            locks = Enumerable.Range(0, NProc).Select(_ => new SemaphoreSlim(1, 1)).ToArray();
            Observable = new SafeObservable<ForkData>();
            Log = log;

            Network.AddHandle(PersistentIn, Session);

            conf.AddCheck(CheckCommitment);
        }

        private (RmcIncoming, Exception?) GetIn(ulong id)
        {
            var success = In.TryGetValue(id, out var result);
            if (success) return (result!, null);

            return (null!, new Exception("unknown incoming"));
        }

        private (RmcInstance, Exception?) GetOut(ulong id)
        {
            var success = Out.TryGetValue(id, out var result);
            if (success) return (result!, null);

            return (null!, new Exception("unknown outgoing"));
        }

        private (RmcInstance, Exception?) Get(ulong id)
        {
            (var incoming, var err) = GetIn(id);
            if (err == null)
            {
                return (incoming, null);
            }

            (var outgoing, err) = GetOut(id);
            if (err == null)
            {
                return (outgoing, null);
            }

            return (null!, new Exception("unknown instance"));
        }

        public Status Status(ulong id)
        {
            (var ins, var err) = Get(id);
            if (err != null) return AuremCore.RMC.Status.Unknown;
            return ins.GetStatus();
        }

        private RmcInstance NewOutgoingInstance(ulong id, byte[] data)
        {
            var res = RmcInstance.NewOutgoing(id, data, Kchain, MyPid, Session);

            return Out.AddOrUpdate(id, res, (a, x) => x);
        }

        private (RmcIncoming, Exception?) NewIncomingInstance(ulong id, ushort pid)
        {
            var res = new RmcIncoming(id, pid, Kchain, MyPid, Session);

            var success = In.AddOrUpdate(id, res, (a, x) => x);
            return (success, success != res ? new Exception("duplicate incoming") : null);
        }

        public MultiSignature? Proof(ulong id)
        {
            (var ins, var err) = Get(id);
            if (err != null) return null;

            return ins.Proof;
        }

        public byte[] Data(ulong id)
        {
            (var ins, var err) = Get(id);
            if (err != null) return Array.Empty<byte>();

            return ins.Data();
        }

        public async Task<(byte[], Exception?)> RmcAcceptFinished(ulong id, ushort pid, RmcSendFinished fin)
        {
            (var ins, var err) = GetIn(id);
            if (err != null)
            {
                (ins, _) = NewIncomingInstance(id, pid);
            }

            return await ins.AcceptFinished(fin);
        }

        public async Task<(Packet, Exception?)> RmcSendFinished(ulong id)
        {
            (var ins, var err) = Get(id);
            if (err != null)
            {
                return (null!, err);
            }

            return (await ins.SendFinished(), null);
        }

        public async Task<(Packet, Exception?)> RmcSendProof(ulong id)
        {
            (var ins, var err) = Get(id);
            if (err != null)
            {
                return (null!, err);
            }

            return (await ins.SendProof(), null);
        }

        public async Task<(bool, Exception?)> RmcAcceptSignature(ulong id, ushort pid, RmcSignature s)
        {
            (var ins, var err) = Get(id);
            if (err != null)
            {
                return (false, err);
            }

            return await ins.AcceptSignature(pid, s.Sig);
        }

        public async Task<(Packet, Exception?)> RmcSendData(ulong id, byte[] data)
        {
            if (Status(id) != AuremCore.RMC.Status.Unknown)
            {
                (var outs, var err) = GetOut(id);
                if (err != null)
                {
                    return (null!, err);
                }

                return (await outs.SendData(), null);
            }
            else
            {
                var outs = NewOutgoingInstance(id, data);
                return (await outs.SendData(), null);
            }
        }

        public async Task<Exception?> RmcAcceptProof(ulong id, RmcProof prf)
        {
            (var ins, var err) = Get(id);
            if (err != null)
            {
                return err;
            }

            return await ins.AcceptProof(prf);
        }

        public async Task<(Packet, Exception?)> RmcSendSignature(ulong id)
        {
            (var ins, var err) = Get(id);
            if (err != null)
            {
                return (null!, err);
            }

            return await ins.SendSignature();
        }

        public async Task<(byte[], Exception?)> RmcAcceptData(ulong id, ushort pid, RmcSendData d)
        {
            (var ins, var err) = GetIn(id);
            if (err != null)
            {
                (ins, err) = NewIncomingInstance(id, pid);
            }

            if (err != null)
            {
                return (Array.Empty<byte>(), err);
            }

            return await ins.AcceptData(d);
        }

        public static long HashToInt64(Hash h)
        {
            long result = 0;
            for (int p = 0; p < h.Length; p++)
            {
                result += h.Data[p] * (1L << p);
            }

            return result;
        }

        public static int HashToInt32(Hash h)
        {
            var result = HashToInt64(h);
            return (int)(result >> 32) + ((int)result & 0x7FFFFFFF);
        }

        public async Task Lock(ushort pid) => await locks[pid].WaitAsync();

        public void Unlock(ushort pid) => locks[pid].Release();

        public async Task PersistentIn(Packet packet)
        {
            try
            {
                switch ((PacketID)packet.Header.PacketID)
                {
                    case PacketID.RmcSendData:
                        {
                            var p = (packet.Body as RmcSendData)!;

                            var pid = p.Greet.Pid;
                            var id = p.Greet.Id;

                            var log = Log.With().Val(Logging.Constants.PID, pid).Val(Logging.Constants.ISID, id).Logger();
                            log.Info().Msg(Logging.Constants.SyncStarted);

                            await AcceptAlert(id, pid, log, p);
                        }
                        break;
                    case PacketID.RmcSignature:
                        {
                            var p = (packet.Body as RmcSignature)!;

                            var pid = p.Pid;
                            var id = p.Id;
                            var key = new SigKey(id, pid);

                            var success = MemberSigTasks.TryGetValue(key, out var val);
                            if (!MemberSigTasks.ContainsKey(key) || !success)
                            {
                                Log.Error().Str("where", "AlertHandler.In.RmcSignature").Val(Logging.Constants.PID, pid).Val(Logging.Constants.OSID, id).Msg("received unsolicited signature");
                                return;
                            }

                            val!.SetResult(p);
                        }
                        break;
                    case PacketID.RmcSendProof:
                        {
                            var p = (packet.Body as RmcSendProof)!;

                            var pid = p.Greet.Pid;
                            var id = p.Greet.Id;

                            var log = Log.With().Val(Logging.Constants.PID, pid).Val(Logging.Constants.ISID, id).Logger();
                            log.Info().Msg(Logging.Constants.SyncStarted);

                            await AcceptProof(id, log, p);
                        }
                        break;
                    case PacketID.RmcSendFinished:
                        {
                            var p = (packet.Body as RmcSendFinished)!;

                            var pid = p.SendData.Greet.Pid;
                            var id = p.SendData.Greet.Id;

                            var log = Log.With().Val(Logging.Constants.PID, pid).Val(Logging.Constants.ISID, id).Logger();
                            log.Info().Msg(Logging.Constants.SyncStarted);

                            await AcceptFinished(id, pid, p, log);
                        }
                        break;
                    case PacketID.RequestComm:
                        {
                            var p = (packet.Body as RequestComm)!;

                            var pid = p.Greet.Pid;
                            var id = p.Greet.Id;

                            var log = Log.With().Val(Logging.Constants.PID, pid).Val(Logging.Constants.ISID, id).Logger();
                            log.Info().Msg(Logging.Constants.SyncStarted);

                            await HandleCommitmentRequest(pid, p, log);
                        }
                        break;
                    case PacketID.CommResp:
                        {
                            var p = (packet.Body as AlertRequestCommitment)!;

                            var key = new ReqKey(p.Hash, p.Pid);

                            var success = Requests.TryGetValue(key, out var val);
                            if (!Requests.ContainsKey(key) || !success)
                            {
                                Log.Error().Str("where", "AlertHandler.In.CommResp").Val(Logging.Constants.PID, p.Pid).Val(Logging.Constants.OSID, 0).Msg("received unsolicited commitment");
                                return;
                            }

                            val!.SetResult(p);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error().Str("where", "AlertHandler.In").Msg(ex.Message);
            }
        }

        //public async Task HandleIncoming(Conn conn)
        //{
        //    try
        //    {
        //        (var pid, var id, var msgType) = await Greetings.AcceptGreeting(conn);

        //        var log = Log.With().Val(Logging.Constants.PID, pid).Val(Logging.Constants.ISID, id).Logger();
        //        log.Info().Msg(Logging.Constants.SyncStarted);

        //        switch ((AlertState)msgType)
        //        {
        //            case AlertState.Alert:
        //                await AcceptAlert(id, pid, conn, log);
        //                break;
        //            case AlertState.Proving:
        //                await AcceptProof(id, conn, log);
        //                break;
        //            case AlertState.Finished:
        //                await AcceptFinished(id, pid, conn, log);
        //                break;
        //            case AlertState.Request:
        //                await HandleCommitmentRequest(conn, log);
        //                break;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error().Str("where", "AlertHandler.HandleIncoming.AcceptGreeting").Msg(ex.Message);
        //    }
        //    finally
        //    {
        //        await conn.Close();
        //    }
        //}

        public (ushort, ushort, uint, Exception?) DecodeAlertID(ulong id, ushort pid)
        {
            var raiser = (ushort)(id & 0xFFFF);
            id >>= 16;
            var forker = (ushort)(id & 0xFFFF);
            id >>= 16;
            var epochID = (uint)id;

            Exception? err = null;
            if (raiser != pid)
            {
                err = new Exception("decoded pid does not match provided pid");
            }
            else if (raiser == forker)
            {
                err = new Exception("cannot commit to own fork");
            }

            return (forker, raiser, epochID, err);
        }

        public async Task AcceptFinished(ulong id, ushort pid, RmcSendFinished p, Logger log)
        {
            (var forker, _, var epochID, var err) = DecodeAlertID(id, pid);
            if (err != null)
            {
                log.Error().Str("where", "AlertHandler.AcceptFinished.DecodeAlertID").Msg(err.Message);
                return;
            }

            (var data, err) = await RmcAcceptFinished(id, pid, p);
            if (err != null)
            {
                log.Error().Str("where", "AlertHandler.AcceptFinished.AcceptData").Msg(err.Message);
                return;
            }

            ForkingProof proof = new ForkingProof();
            try
            {
                proof = proof.Unmarshal(data);
            }
            catch (Exception e)
            {
                log.Error().Str("where", "AlertHandler.AcceptFinished.Unmarshal").Msg(e.Message);
                return;
            }

            var comm = proof.ExtractCommitment(id);
            Commitments.Add(comm, pid, forker);

            await locks[forker].WaitAsync();

            try
            {
                if (Commitments.GetByParties(MyPid, pid) == null)
                {
                    var maxes = (await Orderer.MaxUnits(epochID)).Get(forker);
                    if (maxes == null || maxes.Count == 0)
                    {
                        proof.ReplaceCommit(null);
                    }
                    else
                    {
                        proof.ReplaceCommit(maxes[0]);
                    }

                    await RaiseAlert(proof);
                }
            }
            finally
            {
                locks[forker].Release();
            }
        }

        public async Task SendFinished(ushort forker, ushort pid)
        {
            var comm = Commitments.GetByParties(MyPid, forker);
            if (comm == null)
            {
                Log.Error().Str("where", "AlertHandler.SendFinished.GetByParties").Msg("attempting to prove forking before alert finished");
                return;
            }

            var id = comm.RmcID();
            var log = Log.With().Val(Logging.Constants.PID, pid).Val(Logging.Constants.OSID, id).Logger();

            try
            {
                (var p, var err) = await RmcSendFinished(id);
                if (err != null)
                {
                    log.Error().Str("where", "AlertHandler.SendFinished.SendFinished").Msg(err.Message);
                    return;
                }

                try
                {
                    Network.Send(pid, p);
                }
                catch (Exception ex)
                {
                    log.Error().Str("where", "AlertHandler.SendFinished.Flush").Msg(ex.Message);
                }
            }
            finally
            {
            }
        }

        public async Task<(ICommitment, Exception?)> ProduceCommitmentFor(IUnit unit)
        {
            var comm = Commitments.GetByParties(MyPid, unit.Creator());
            if (comm == null)
            {
                return (null!, new Exception("no fork can be found in relation to this unit"));
            }

            var pu = comm.GetUnit();
            if (pu == null)
            {
                return (null!, new Exception("commitment invalid; no commitment to a unit"));
            }

            var commUnit = (await Orderer.UnitsByHash(pu.Hash()))[0];
            if (commUnit == null)
            {
                return (null!, new Exception("the unit committed to is not present"));
            }

            var pred = commUnit.Predecessor();
            while (pred != null && HasCommitmentTo(pred))
            {
                commUnit = pred;
                pred = pred.Predecessor();
            }

            if (pred == null || commUnit.Height() <= unit.Height())
            {
                // apparently the commitment was added during the meantime.
                comm = Commitments.GetByHash(unit.Hash());
                if (comm == null)
                {
                    return (null!, new Exception("this unit has no commitment"));
                }
            }
            else
            {
                Exception? err;

                comm = Commitments.GetByHash(commUnit.Hash());
                while (commUnit.Height() > unit.Height())
                {
                    (comm, err) = BaseCommitment.CommitmentForParent(comm, commUnit);
                    if (err != null)
                    {
                        return (null!, err);
                    }

                    Commitments.Add(comm, MyPid, commUnit.Creator());
                    commUnit = commUnit.Predecessor();
                }

                var cu = comm.GetUnit();
                if (cu == null || cu.Hash() != unit.Hash())
                {
                    return (null!, new Exception("produced commitment for wrong unit"));
                }
            }

            return (comm, null);
        }

        public async Task HandleCommitmentRequest(ushort requester, RequestComm req, Logger log)
        {
            Hash requested = req.Hash;

            var unit = (await Orderer.UnitsByHash(requested))[0];
            if (unit == null)
            {
                log.Error().Str("where", "AlertHandler.HandleCommitmentRequest.Get").Msg("no commitment for unit not in orderer");
                return;
            }

            // we always want to send one commitment more if we can, so that we send the parents' hashes to add the unit.
            var pred = unit.Predecessor();
            if (pred != null)
            {
                unit = pred;
            }

            Exception? err;
            var comm = Commitments.GetByHash(requested);
            if (comm == null)
            {
                if (!IsForker(unit.Creator()))
                {
                    log.Error().Str("where", "AlertHandler.HandleCommitmentRequest.GetByHash").Msg("alerter was unaware a fork was present");

                    string worf = "Write";
                    try
                    {
                        Network.Send(requester, new Packet(PacketID.CommResp, new AlertRequestCommitment(requested, MyPid, 1), Session));
                    }
                    catch (Exception ex)
                    {
                        log.Error().Str("where", $"AlertHandler.HandleCommitmentRequest.{worf}").Msg(ex.Message);
                    }

                    return;
                }

                (comm, err) = await ProduceCommitmentFor(unit);
                if (err != null)
                {
                    log.Error().Str("where", "AlertHandler.HandleCommitmentRequest.ProduceCommitmentFor").Msg(err.Message);
                    return;
                }
            }

            string f = "Write";
            try
            {
                f = "SendFinished";
                (var pf, var exc) = await RmcSendFinished(comm.RmcID());
                if (exc != null) throw exc;

                f = "Write";
                var p = new Packet(PacketID.CommResp, new AlertRequestCommitment(requested, MyPid, 0, comm, (pf.Body as RmcSendFinished)!), Session);
                Network.Send(requester, p);
            }
            catch (Exception ex)
            {
                log.Error().Str("where", $"AlertHandler.HandleCommitmentRequest.{f}").Msg(ex.Message);
                return;
            }

            log.Info().Msg(Logging.Constants.SyncCompleted);
        }

        public async Task<Exception?> RequestCommitment(IPreunit pu, ushort pid)
        {
            var log = Log.With().Val(Logging.Constants.PID, pid).Logger();
            log.Info().Msg(Logging.Constants.SyncStarted);

            try
            {
                var p = new Packet(PacketID.RequestComm, new RequestComm(0, MyPid, (byte)AlertState.Request, pu.Hash()), Session);

                var t = Requests.GetOrAdd(new ReqKey(pu.Hash(), pid), _ => new TaskCompletionSource<AlertRequestCommitment>());
                
                CancellationTokenSource cts = new CancellationTokenSource();
                cts.CancelAfter(5000);
                Network.Send(pid, p);

                var res = await t.Task.WaitAsync(cts.Token);
                cts.Dispose();

                if (res.Unknown == 1)
                {
                    await SendFinished(pu.Creator(), pid);
                    return new Exception("peer was unaware of forker");
                }

                (var comms, var err) = (res.DecodedComms, res.DecodeException);
                if (err != null)
                {
                    log.Error().Str("where", "alertHandler.RequestCommitment.AcquireCommitments").Msg(err.Message);
                    return err;
                }

                (_, var raiser, _, _) = DecodeAlertID(comms[0].RmcID(), 0);
                (var data, err) = await RmcAcceptFinished(comms[0].RmcID(), raiser, res.Finished!);
                if (err != null)
                {
                    log.Error().Str("where", "alertHandler.RequestCommitment.AcceptFinished").Msg(err.Message);
                    return err;
                }

                ForkingProof proof = new();
                try
                {
                    proof = proof.Unmarshal(data);
                }
                catch (Exception ex)
                {
                    return ex;
                }

                err = Commitments.AddBatch(comms, proof, raiser);
                if (err != null)
                {
                    log.Error().Str("where", "alertHandler.RequestCommitment.AddBatch").Msg(err.Message);
                    return err;
                }

                log.Info().Msg(Logging.Constants.SyncCompleted);
                return null;
            }
            catch (OperationCanceledException e)
            {
                log.Error().Msg("receive timed out");
                return e;
            }
            finally
            {
            }
        }

        public async Task AcceptAlert(ulong id, ushort pid, Logger log, RmcSendData p)
        {
            (var forker, _, var epochID, var err) = DecodeAlertID(id, pid);
            if (err != null)
            {
                log.Error().Str("where", "AlertHandler.AcceptAlert.DecodeAlertID").Msg(err.Message);
                return;
            }

            (var data, err) = await RmcAcceptData(id, pid, p);
            if (err != null)
            {
                log.Error().Str("where", "AlertHandler.AcceptAlert.AcceptData").Msg(err.Message);
                return;
            }

            ForkingProof proof = new();
            try
            {
                proof = proof.Unmarshal(data);
            }
            catch (Exception e)
            {
                log.Error().Str("where", "AlertHandler.AcceptAlert.Unmarshal").Msg(e.Message);
                return;
            }

            err = proof.CheckCorrectness(forker, Keys[forker]);
            if (err != null)
            {
                log.Error().Str("where", "AlertHandler.AcceptAlert.CheckCorrectness").Msg(err.Message);
                return;
            }

            var comm = proof.ExtractCommitment(id);
            Commitments.Add(comm, pid, forker);
            err = await MaybeSign(id, pid);
            if (err != null)
            {
                log.Error().Str("where", "AlertHandler.AcceptAlert.MaybeSign").Msg(err.Message);
            }
            else
            {
                log.Info().Msg(Logging.Constants.SyncCompleted);
            }

            await locks[forker].WaitAsync();

            try
            {
                if (Commitments.GetByParties(MyPid, pid) == null)
                {
                    var maxes = (await Orderer.MaxUnits(epochID)).Get(forker);
                    if (maxes == null || maxes.Count == 0)
                    {
                        proof.ReplaceCommit(null);
                    }
                    else
                    {
                        proof.ReplaceCommit(maxes[0]);
                    }

                    await RaiseAlert(proof);
                }
            }
            finally
            {
                locks[forker].Release();
            }
        }

        public async Task<Exception?> MaybeSign(ulong id, ushort pid)
        {
            (var p, var err) = await RmcSendSignature(id);
            if (err != null)
            {
                return err;
            }

            try
            {
                Network.Send(pid, p);
                return null;
            }
            catch (Exception e)
            {
                return e;
            }
        }

        public async Task AcceptProof(ulong id, Logger log, RmcSendProof p)
        {
            var err = await RmcAcceptProof(id, p.Proof);
            if (err != null)
            {
                log.Error().Str("where", "AlertHandler.AcceptProof.AcceptProof").Msg(err.Message);
            }
        }

        private void CheckCommitment(IUnit u, IDag _)
        {
            if (HandleForkerUnit(u).GetAwaiter().GetResult() && !HasCommitmentTo(u))
            {
                throw new NoCommitmentException("missing commitment to fork");
            }
        }

        private bool HasCommitmentTo(IUnit u)
        {
            var comm = Commitments.GetByHash(u.Hash());
            return comm != null;
        }

        public bool IsForker(ushort forker) => Commitments.IsForker(forker);

        private async Task<bool> HandleForkerUnit(IUnit u)
        {
            var creator = u.Creator();
            if (IsForker(creator))
            {
                return true;
            }

            var _mm = await Orderer.MaxUnits(u.EpochID());
            var maxes = _mm.Get(creator);
            if (maxes == null || maxes.Count == 0)
            {
                return false;
            }

            // we can only have one max because the creator is not a forker yet.
            var max = maxes.First();
            if (max.Height() >= u.Height())
            {
                var v = max;
                while (v.Height() > u.Height())
                {
                    v = v.Predecessor();
                }

                var proof = new ForkingProof(u, v, max);
                if (proof.IsNull())
                {
                    return false;
                }

                // alert raise must be pinned
                await RaiseAlert(proof);
                return true;
            }

            return false;
        }

        public async Task RaiseAlert(ForkingProof proof)
        {
            if (Commitments.GetByParties(MyPid, proof.ForkerID()) != null)
            {
                // We already committed at some point, so no reason to commit a second time.
                return;
            }

            var wg = new WaitGroup();
            var gathering = new WaitGroup();
            var id = AlertID(proof.ForkerID(), proof.EpochID());
            var data = proof.Marshal();

            for (ushort pid = 0; pid < NProc; pid++)
            {
                if (pid == MyPid || pid == proof.ForkerID())
                {
                    continue;
                }

                wg.Add(1);
                gathering.Add(1);
                var _pid = pid;
                _ = Task.Run(async () =>
                {
                    await SendAlert(data, id, _pid, gathering, wg);
                });
            }

            await wg.WaitAsync();

            var comm = proof.ExtractCommitment(id);
            Commitments.Add(comm, MyPid, proof.ForkerID());
        }

        public async Task<Exception?> AttemptProve(ushort recipient, ulong id)
        {
            try
            {
                (var p, var err) = await RmcSendProof(id);
                if (err != null) return err;

                Network.Send(recipient, p);
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
            finally
            {

            }
        }

        public async Task<(IUnit, Exception?)> Disambiguate(IUnit[] possibleParents, IPreunit pu)
        {
            if (possibleParents != null || possibleParents.Length == 0)
            {
                return (null, null);
            }

            if (possibleParents.Length == 1)
            {
                return (possibleParents[0], null);
            }

            var pid = pu.Creator();
            var forker = possibleParents[0].Creator();
            if (pid == forker)
            {
                return DisambiguateForker(possibleParents, pu);
            }

            var height = possibleParents[0].Height();
            var comm = Commitments.GetByParties(pid, forker);
            if (comm != null)
            {
                return (null, new NoCommitmentException("no commitment by this pid"));
            }

            var cu = comm.GetUnit();
            if (cu == null)
            {
                return (null, new ComplianceException("unit built on noncommitted parent"));
            }

            var u = (await Orderer.UnitsByHash(cu.Hash()))?[0];
            if (u == null)
            {
                return (null, new NoCommitmentException("no committed unit needed for disambiguation"));
            }

            if (u.Height() < height)
            {
                return (null, new ComplianceException("unit built on noncommitted parent"));
            }

            while (u.Height() > height)
            {
                u = u.Predecessor();
            }

            return (u, null);
        }

        public (IUnit, Exception?) DisambiguateForker(IUnit[] possibleParents, IPreunit pu)
        {
            var comm = Commitments.GetByHash(pu.Hash());
            if (comm == null)
            {
                return (null, new NoCommitmentException("missing commitment to fork"));
            }

            var h = comm.GetParentHash(pu.Creator());
            if (h == Hash.Empty || h == Hash.ZeroHash)
            {
                return (null, new Exception("too shallow commitment"));
            }

            foreach (var u in possibleParents)
            {
                if (h == u.Hash())
                {
                    return (u, null);
                }
            }

            return (null, new UnknownParentsException(1));
        }

        public async Task SendAlert(byte[] data, ulong id, ushort pid, WaitGroup wg, WaitGroup gathering)
        {
            try
            {
                var success = false;
                var log = Log.With().Val(Logging.Constants.PID, pid).Val(Logging.Constants.OSID, id).Logger();
                var delay = 5000;

                while (Status(id) != AuremCore.RMC.Status.Finished)
                {
                    log.Info().Msg(Logging.Constants.SyncStarted);

                    try
                    {
                        // create packet
                        (var p, var err) = await RmcSendData(id, data);
                        if (err != null)
                        {
                            log.Error().Str("where", "AlertHandler.SendAlert.SendData").Msg(err.Message);
                            await Task.Delay(50);
                            continue;
                        }

                        // register awaiter
                        var t = MemberSigTasks.GetOrAdd(new SigKey(id, pid), _ => new TaskCompletionSource<RmcSignature>());

                        // send
                        CancellationTokenSource cts = new CancellationTokenSource();
                        cts.CancelAfter(delay);
                        Network.Send(pid, p);

                        // wait for result or timeout
                        var res = await t.Task.WaitAsync(cts.Token);
                        cts.Dispose();

                        (_, err) = await RmcAcceptSignature(id, pid, res);
                        if (err != null)
                        {
                            log.Error().Str("where", "AlertHandler.SendAlert.AttemptGather").Msg(err.Message);
                            //await Task.Delay(50);
                            //continue;
                        }
                        else
                        {
                            log.Info().Msg(Logging.Constants.SyncCompleted);
                            success = true;
                            break;
                        }
                    }
                    catch (OperationCanceledException e)
                    {
                        log.Error().Str("where", "AlertHandler.SendAlert.AttemptGather").Msg("timed out; doubling delay");
                        delay *= 2;
                        continue;
                    }
                }

                gathering.Done();
                await gathering.WaitAsync();

                if (success)
                {
                    var err = await AttemptProve(pid, id);
                    if (err != null)
                    {
                        log.Error().Str("where", "AlertHandler.SendAlert.AttemptProve").Msg(err.Message);
                    }
                }
            }
            finally
            {
                wg.Done();
            }
        }

        public ulong AlertID(ushort forker, uint epochID)
        {
            var result = (ulong)MyPid;
            result += (ulong)forker << 16;
            result += (ulong)epochID << 32;

            return result;
        }

        public async Task<Exception?> ResolveMissingCommitment(Exception? e, IPreunit pu, ushort source)
        {
            if (e != null)
            {
                if (e is NoCommitmentException)
                {
                    var err = await RequestCommitment(pu, source);
                    if (err != null) return err;
                }
                else
                {
                    return e;
                }
            }

            return null;
        }

        public async Task NewFork(IPreunit u, IPreunit v)
        {
            if (u.UnitID() != v.UnitID()) return;

            await locks[u.Creator()].WaitAsync();

            try
            {
                if (IsForker(u.Creator()))
                {
                    return;
                }

                var maxes = (await Orderer.MaxUnits(u.EpochID())).Get(u.Creator());

                // there can only be one unit in maxes, since its creator is not a forker
                IUnit? max = null;
                if (maxes != null && maxes.Count > 0)
                {
                    max = maxes[0];
                }

                var proof = new ForkingProof(u, v, max);
                await RaiseAlert(proof);
                NotifyObservers(u, v);
            }
            finally
            {
                locks[u.Creator()].Release();
            }
        }

        public sealed class ForkData
        {
            public IPreunit first, second;
        }

        public IObserverManager AddForkObserver(Action<IPreunit, IPreunit> observer) => Observable.AddObserver(x => observer(x.first, x.second));

        public void NotifyObservers(IPreunit u, IPreunit v) => Observable.Notify(new ForkData { first = u, second = v });
    }
}
