using Aurem.Logging;
using Aurem.Model;
using Aurem.Model.Exceptions;
using Aurem.Serialize;
using Aurem.Syncing.Internals;
using Aurem.Syncing.Internals.Packets;
using Aurem.Syncing.Internals.Packets.Bodies;
using AuremCore.Core;
using AuremCore.Core.Extensions;
using AuremCore.Crypto.Multi;
using AuremCore.FastLogger;
using AuremCore.RMC;
using BN256Core.Extensions;
using Nito.AsyncEx;
using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Syncing
{
    public class RmcServer : IService
    {
        public class RmcInstance
        {
            protected SemaphoreSlim Mutex = new SemaphoreSlim(1, 1);

            public ulong Id;
            public Keychain Keys;
            public MultiSignature Proof;

            protected ushort MyPid;
            protected uint RawLen;
            protected byte[] SignedData;
            protected Status Stat;

            public RmcInstance() { }

            public static RmcInstance NewOutgoing(ulong id, byte[] data, Keychain keys, ushort myPid)
            {
                var rawLen = (uint)data.Length;
                var buf = new byte[8 + rawLen];
                BinaryPrimitives.WriteUInt64LittleEndian(buf, id);
                Buffer.BlockCopy(data, 0, buf, 8, data.Length);
                var signedData = buf.Concat(keys.Sign(buf));
                var nproc = (ushort)keys.Length;
                var proof = new MultiSignature(IDag.MinimalQuorum(nproc), signedData);

                proof.Aggregate(keys.Pid(), keys.Sign(signedData));

                return new RmcInstance
                {
                    Id = id,
                    Keys = keys,
                    RawLen = rawLen,
                    SignedData = signedData,
                    Proof = proof,
                    Stat = AuremCore.RMC.Status.Data,
                    MyPid = myPid,
                };
            }

            public Status GetStatus()
            {
                Mutex.Wait();

                try
                {
                    return Stat;
                }
                finally
                {
                    Mutex.Release();
                }
            }

            public byte[] Data()
            {
                if (RawLen == SignedData.Length) return SignedData;
                
                return SignedData[8..(int)(8 + RawLen)];
            }

            public async Task<Packet> SendData()
            {
                await Mutex.WaitAsync();

                try
                {
                    Packet p = new Packet(PacketID.RmcSendData, new RmcSendData(MyPid, Id, MsgSendData, SignedData));
                    return p;
                }
                finally
                {
                    Mutex.Release();
                }
            }

            public async Task<Packet> SendProof()
            {
                await Mutex.WaitAsync();

                try
                {
                    Packet p = new Packet(PacketID.RmcSendProof, new RmcSendProof(MyPid, Id, MsgSendProof, Proof.Marshal()));
                    return p;
                }
                finally
                {
                    Mutex.Release();
                }
            }

            public async Task<Packet> SendFinished()
            {
                var d = await SendData();
                var db = d.Body as RmcSendData;
                var _p = await SendProof();
                var _pb = _p.Body as RmcSendProof;
                Packet p = new Packet(PacketID.RmcSendFinished, new RmcSendFinished(db!, _pb!.Proof));
                return p;
            }

            public async Task<(bool, Exception?)> AcceptSignature(ushort pid, byte[] sig)
            {
                if (sig.Length != Keychain.SignatureLength)
                {
                    return (false, new Exception("malformed signature"));
                }

                await Mutex.WaitAsync();

                try
                {
                    if (!Keys.Verify(pid, SignedData.Concat(sig)))
                    {
                        return (false, new Exception("wrong signature"));
                    }

                    if (Stat != AuremCore.RMC.Status.Finished)
                    {
                        (var done, var err) = Proof.Aggregate(pid, sig);
                        if (done)
                        {
                            Stat = AuremCore.RMC.Status.Finished;
                            return (true, err);
                        }

                        return (false, err);
                    }

                    return (false, null);
                }
                finally
                {
                    Mutex.Release();
                }
            }

            public async Task<(Packet, Exception?)> SendSignature()
            {
                await Mutex.WaitAsync();

                try
                {
                    if (Stat == AuremCore.RMC.Status.Unknown)
                    {
                        return (null!, new Exception("cannot sign unknown data"));
                    }

                    var sig = Keys.Sign(SignedData);
                    
                    if (Stat == AuremCore.RMC.Status.Data)
                    {
                        Stat = AuremCore.RMC.Status.Signed;
                    }

                    Packet p = new Packet(PacketID.RmcSignature, new RmcSignature(MyPid, Id, sig));
                    return (p, null);
                }
                catch (Exception e)
                {
                    return (null!, e);
                }
                finally
                {
                    Mutex.Release();
                }
            }

            public async Task<Exception?> AcceptProof(RmcProof prf)
            {
                await Mutex.WaitAsync();

                try
                {
                    if (Stat == AuremCore.RMC.Status.Unknown)
                    {
                        throw new Exception("cannot accept proof of unknown data");
                    }

                    var nproc = (ushort)Keys.Length;
                    var proof = new MultiSignature(IDag.MinimalQuorum(nproc), SignedData);
                    var data = prf.Data;

                    if (data.Length != proof.Length)
                    {
                        throw new Exception("received less than the expected number of bytes");
                    }

                    proof.Unmarshal(data);

                    if (!Keys.MultiVerify(proof))
                    {
                        throw new Exception("wrong multisignature");
                    }

                    if (Stat != AuremCore.RMC.Status.Finished)
                    {
                        Proof = proof;
                        Stat = AuremCore.RMC.Status.Finished;
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    return ex;
                }
                finally
                {
                    Mutex.Release();
                }
            }
        }

        public class RmcIncoming : RmcInstance
        {
            public ushort Pid;

            public RmcIncoming(ulong id, ushort pid, Keychain keys, ushort mypid)
            {
                Id = id;
                Keys = keys;
                Pid = pid;
                MyPid = mypid;
            }

            public async Task<(byte[], Exception?)> AcceptData(RmcSendData _sendData)
            {
                try
                {
                    RmcData _data = _sendData.Data;
                    var signedData = _data.Data;
                    var rawLen = signedData.Length - 8 - Keychain.SignatureLength;

                    if (signedData.Length < 8 + Keychain.SignatureLength)
                    {
                        return (Array.Empty<byte>(), new Exception("received data is too short"));
                    }

                    var id = BinaryPrimitives.ReadUInt64LittleEndian(signedData);
                    if (id != Id)
                    {
                        return (Array.Empty<byte>(), new Exception("incoming id mismatch"));
                    }

                    if (!Keys.Verify(Pid, signedData))
                    {
                        return (Array.Empty<byte>(), new Exception("wrong data signature"));
                    }

                    var nproc = (ushort)Keys.Length;
                    var proof = new MultiSignature(IDag.MinimalQuorum(nproc), signedData);

                    await Mutex.WaitAsync();
                    
                    try
                    {
                        if (Stat == AuremCore.RMC.Status.Unknown)
                        {
                            Stat = AuremCore.RMC.Status.Data;
                        }
                        else
                        {
                            var thisData = signedData[8..(8 + rawLen)];
                            if (!thisData.BEquals(Data()))
                            {
                                return (Array.Empty<byte>(), new Exception("diferent data already accepted"));
                            }

                            return (Data(), null);
                        }

                        SignedData = signedData;
                        RawLen = (uint)rawLen;
                        Proof = proof;

                        return (Data(), null);
                    }
                    catch (Exception ex)
                    {
                        return (Array.Empty<byte>(), ex);
                    }
                    finally
                    {
                        Mutex.Release();
                    }
                }
                catch (Exception e)
                {
                    return (Array.Empty<byte>(), e);
                }
            }

            public async Task<(byte[], Exception?)> AcceptFinished(RmcSendFinished f)
            {
                (var res, var err) = await AcceptData(f.SendData);
                if (err != null) return (res, err);

                return (res, await AcceptProof(f.Proof));
            }
        }

        public struct SigKey
        {
            public ulong Id;
            public ushort Recipient;

            public SigKey(ulong id, ushort recipient)
            {
                Id = id;
                Recipient = recipient;
            }

            public override bool Equals([NotNullWhen(true)] object? obj)
            {
                if (obj is SigKey other)
                {
                    return other.Id == Id && other.Recipient == Recipient;
                }
                else
                {
                    return false;
                }
            }

            public override int GetHashCode()
            {
                return (int)Id + (int)(Id >> 32) + (int)(Recipient << 16);
            }
        }

        public class SigKeyEqualityComparer : IEqualityComparer<SigKey>
        {
            public bool Equals(SigKey x, SigKey y)
            {
                return x.Equals(y);
            }

            public int GetHashCode([DisallowNull] SigKey obj)
            {
                return obj.GetHashCode();
            }
        }

        public const string RmcMismatch = "unit differs from successfully RMC'd unit";
        public const byte MsgSendData = 0;
        public const byte MsgSendProof = 1;
        public const byte MsgRequestFinished = 2;

        protected ushort Pid;
        protected ushort NProc;
        protected IOrderer Orderer;
        protected Network Netserv;
        protected Logger Log;

        protected ConcurrentDictionary<ulong, RmcInstance> Out;
        protected ConcurrentDictionary<ulong, RmcIncoming> In;
        protected ConcurrentDictionary<SigKey, TaskCompletionSource<RmcSendFinished>> Finished;
        protected ConcurrentDictionary<SigKey, TaskCompletionSource<RmcSignature>> MemberSigTasks;
        protected Keychain Keys;

        protected SemaphoreSlim multicastInProgress;
        protected bool Quit;
        protected AsyncReaderWriterLock Mx;
        protected WaitGroup Wg;

        protected RmcServer(Config.Config conf, IOrderer orderer, Network netserv, Logger log)
        {
            NProc = conf.NProc;
            Pid = conf.Pid;
            Orderer = orderer;
            Netserv = netserv;
            Log = log;

            Out = new ConcurrentDictionary<ulong, RmcInstance>();
            In = new ConcurrentDictionary<ulong, RmcIncoming>();
            Finished = new ConcurrentDictionary<SigKey, TaskCompletionSource<RmcSendFinished>>(new SigKeyEqualityComparer());
            MemberSigTasks = new ConcurrentDictionary<SigKey, TaskCompletionSource<RmcSignature>>(new SigKeyEqualityComparer());
            Keys = new Keychain(conf.RMCPublicKeys, conf.RMCPrivateKey);

            Netserv.OnReceive += PersistentIn;
            conf.AddCheck((u, d) => FinishedRMC(u, d).GetAwaiter().GetResult());
            
            multicastInProgress = new SemaphoreSlim(1, 1);
            Quit = false;
            Mx = new AsyncReaderWriterLock();
            Wg = new WaitGroup();
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
            var res = RmcInstance.NewOutgoing(id, data, Keys, Pid);

            return Out.AddOrUpdate(id, res, (a, x) => x);
        }

        private (RmcIncoming, Exception?) NewIncomingInstance(ulong id, ushort pid)
        {
            var res = new RmcIncoming(id, pid, Keys, Pid);

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

        public static (IService, Requests.Multicast) NewServer(Config.Config conf, IOrderer orderer, Network netserv, Logger log)
        {
            var s = new RmcServer(conf, orderer, netserv, log);
            return (s, s.Send);
        }

        public async Task<Exception?> Start()
        {
            await Netserv.Start();
            return null;
        }

        public async Task StopAsync()
        {
            var _lock = await Mx.WriterLockAsync();

            try
            {
                Quit = true;
                await Wg.WaitAsync();
                Netserv.OnReceive -= PersistentIn;
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

        public async Task SendFinished(ulong id, ushort recipient, Logger log)
        {
            if (Status(id) != AuremCore.RMC.Status.Finished)
            {
                log.Error().Str("where", "Rmc.In.SendFinished").Msg("requested to send finished before RMC reached the finished state");
                return;
            }

            (var p, var err) = await RmcSendFinished(id);
            if (err != null)
            {
                log.Error().Str("where", "Rmc.In.SendFinished").Msg(err.Message);
                return;
            }

            Netserv.Send(recipient, p);
        }

        // TODO: use delay scheduler? figure out later.
        public async Task<(IPreunit, Exception?)> FetchFinished(IUnit u, ushort pid)
        {
            try
            {
                var id = u.UnitID();

                // register
                var t = Finished.GetOrAdd(new SigKey(id, pid), _ => new TaskCompletionSource<RmcSendFinished>());

                // send greeting
                var greet = new Packet(PacketID.RmcGreet, new RmcGreet(pid, id, MsgRequestFinished));
                Netserv.Send(pid, greet);

                // wait for finished
                var res = await t.Task;

                // clean up
                Finished.TryRemove(new SigKey(id, pid), out _);

                (var data, var err) = await RmcAcceptFinished(id, pid, res);
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
            catch (Exception e)
            {
                return (null!, e);
            }
        }

        public async Task<(IPreunit, Exception?)> FetchFinishedFromAll(IUnit u)
        {
            // call the creator first
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

        public async Task<Exception?> FinishedRMC(IUnit u, IDag d)
        {
            if (u.Creator() == Pid) return null;

            var rmcID = u.UnitID();
            IPreunit pu;
            Exception? err;

            if (Status(rmcID) != AuremCore.RMC.Status.Finished)
            {
                (pu, err) = await FetchFinishedFromAll(u);
            }
            else
            {
                pu = DelegateExtensions.InvokeAndCaptureException(EncodeUtil.DecodeUnit, Data(rmcID), out err);
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

        public async Task<Exception?> SendProof(ushort recipient, ulong id)
        {
            (var p, var err) = await RmcSendProof(id);
            if (err != null) return err;

            Netserv.Send(recipient, p);
            return null;
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

                        // cleanup
                        MemberSigTasks.TryRemove(new SigKey(id, _pid), out _);
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

        public async Task<bool> GetMemberSignature(byte[] data, ulong id, ushort recipient)
        {
            var log = Log.With().Val(Logging.Constants.PID, recipient).Val(Logging.Constants.OSID, id).Logger();
            var delay = 5000;
            while (Status(id) != AuremCore.RMC.Status.Finished)
            {
                try
                {
                    (var p, var err) = await RmcSendData(id, data);
                    if (err != null)
                    {
                        log.Error().Str("where", "Rmc.GetMemberSignature.SendData").Msg(err.Message);
                        await Task.Delay(50);
                        continue;
                    }

                    // register awaiter
                    var t = MemberSigTasks.GetOrAdd(new SigKey(id, recipient), _ => new TaskCompletionSource<RmcSignature>());

                    // send
                    CancellationTokenSource cts = new CancellationTokenSource();
                    cts.CancelAfter(delay);
                    Netserv.Send(recipient, p);

                    // wait until result or timeout
                    var res = await t.Task.WaitAsync(cts.Token);
                    cts.Dispose();

                    // on result, add signature
                    (_, err) = await RmcAcceptSignature(id, recipient, res);
                    if (err != null)
                    {
                        log.Error().Str("where", "Rmc.GetMemberSignature.AcceptSignature").Msg(err.Message);
                        await Task.Delay(50);
                        continue;
                    }

                    log.Info().Msg(Logging.Constants.SyncCompleted);
                    return true;
                }
                catch (OperationCanceledException e)
                {
                    log.Error().Str("where", "Rmc.GetMemberSignature.AttemptGather").Msg("timed out; doubling delay");
                    delay *= 2;
                    continue;
                }
            }

            return false;
        }

        public async Task AcceptData(ulong id, ushort sender, RmcSendData d, Logger log)
        {
            (_, var creator, _) = IPreunit.DecodeID(id);
            if (creator != sender)
            {
                log.Error().Str("where", "Rmc.In.AcceptData").Msg("pid and id mismatch");
                return;
            }

            (var data, var err) = await RmcAcceptData(id, sender, d);
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

            (var p, err) = await RmcSendSignature(id);
            if (err != null)
            {
                log.Error().Str("where", "Rmc.In.SendSignature").Msg(err.Message);
                return;
            }

            Netserv.Send(sender, p);
        }

        public async Task<bool> AcceptProof(ulong id, Logger log, RmcSendProof p)
        {
            var err = await RmcAcceptProof(id, p.Proof);
            if (err != null)
            {
                log.Error().Str("where", "Rmc.In.AcceptProof").Msg(err.Message);
                return false;
            }

            return true;
        }

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

                            await AcceptData(id, pid, p, log);

                            log.Info().Msg(Logging.Constants.SyncCompleted);
                        }
                        break;
                    case PacketID.RmcSendProof:
                        {
                            var p = (packet.Body as RmcSendProof)!;

                            var pid = p.Greet.Pid;
                            var id = p.Greet.Id;

                            var log = Log.With().Val(Logging.Constants.PID, pid).Val(Logging.Constants.ISID, id).Logger();
                            log.Info().Msg(Logging.Constants.SyncStarted);

                            if (await AcceptProof(id, log, p))
                            {
                                var pu = DelegateExtensions.InvokeAndCaptureException(EncodeUtil.DecodeUnit, Data(id), out var err);
                                if (err != null)
                                {
                                    log.Error().Str("where", "Rmc.In.DecodeUnit").Msg(err.Message);
                                    return;
                                }
                                LoggingUtil.AddingErrors(await Orderer.AddPreunits(pu.Creator(), pu), 1, log);
                            }

                            log.Info().Msg(Logging.Constants.SyncCompleted);
                        }
                        break;
                    case PacketID.RmcGreet:
                        {
                            var p = (packet.Body as RmcGreet)!;

                            var pid = p.Pid;
                            var id = p.Id;

                            var log = Log.With().Val(Logging.Constants.PID, pid).Val(Logging.Constants.ISID, id).Logger();
                            log.Info().Msg(Logging.Constants.SyncStarted);

                            if (p.MsgType != MsgRequestFinished)
                            {
                                Log.Error().Str("where", "Rmc.In.RequestFinished").Msg("expected msg type to be MsgRequestFinished");
                            }
                            else
                            {
                                await SendFinished(id, pid, log);
                            }

                            log.Info().Msg(Logging.Constants.SyncCompleted);
                        }
                        break;
                    case PacketID.RmcSignature:
                        {
                            var p = (packet.Body as RmcSignature);

                            var pid = p.Pid;
                            var id = p.Id;
                            var key = new SigKey(id, pid);

                            var success = MemberSigTasks.TryGetValue(key, out var val);
                            if (!MemberSigTasks.ContainsKey(key) || !success)
                            {
                                Log.Error().Str("where", "Rmc.In.RmcSignature").Val(Logging.Constants.PID, pid).Val(Logging.Constants.OSID, id).Msg("received unsolicited signature");
                                return;
                            }

                            val!.SetResult(p);
                        }
                        break;
                    default:
                        Log.Error().Str("where", "Rmc.In.Switch").Msg($"unknown or unexpected Rmc message type {(PacketID)packet.Header.PacketID}");
                        return;
                }
            }
            catch (Exception e)
            {
                Log.Error().Str("where", "Rmc.In").Msg(e.Message);
            }
        }
    }
}
