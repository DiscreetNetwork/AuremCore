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

namespace Aurem.Forking
{
    public class AlertHandler
    {
        public ushort MyPid;
        public ushort NProc;
        public IOrderer Orderer;
        public IPublicKey[] Keys;
        public ReliableMulticast Rmc;
        public Server Netserv;
        public CommitBase Commitments;
        public AuremCore.Utils.IObservable<ForkData> Observable; // FIXME: what type is our observable?

        private Logger Log;
        public readonly SemaphoreSlim[] locks;

        public AlertHandler(Config.Config conf, IOrderer orderer, ReliableMulticast rmc, Server netserv, Logger log)
        {
            MyPid = conf.Pid;
            NProc = conf.NProc;
            Keys = conf.PublicKeys.ToArray();
            Orderer = orderer;
            Rmc = rmc;
            Netserv = netserv;
            Commitments = new CommitBase();
            locks = Enumerable.Range(0, NProc).Select(_ => new SemaphoreSlim(1, 1)).ToArray();
            Observable = new SafeObservable<ForkData>();
            Log = log;

            conf.AddCheck(CheckCommitment);
        }

        public async Task Lock(ushort pid) => await locks[pid].WaitAsync();

        public void Unlock(ushort pid) => locks[pid].Release();

        public async Task HandleIncoming(Conn conn)
        {
            try
            {
                (var pid, var id, var msgType) = await Greetings.AcceptGreeting(conn);

                var log = Log.With().Val(Logging.Constants.PID, pid).Val(Logging.Constants.ISID, id).Logger();
                log.Info().Msg(Logging.Constants.SyncStarted);

                switch ((AlertState)msgType)
                {
                    case AlertState.Alert:
                        await AcceptAlert(id, pid, conn, log);
                        break;
                    case AlertState.Proving:
                        await AcceptProof(id, conn, log);
                        break;
                    case AlertState.Finished:
                        await AcceptFinished(id, pid, conn, log);
                        break;
                    case AlertState.Request:
                        await HandleCommitmentRequest(conn, log);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error().Str("where", "AlertHandler.HandleIncoming.AcceptGreeting").Msg(ex.Message);
            }
            finally
            {
                await conn.Close();
            }
        }

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

        public async Task AcceptFinished(ulong id, ushort pid, Conn conn, Logger log)
        {
            (var forker, _, var epochID, var err) = DecodeAlertID(id, pid);
            if (err != null)
            {
                log.Error().Str("where", "AlertHandler.AcceptFinished.DecodeAlertID").Msg(err.Message);
                return;
            }

            (var data, err) = await Rmc.AcceptFinished(id, pid, conn);
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

            Conn conn;
            try
            {
                conn = (await Netserv.Dial(pid))!;
            }
            catch
            {
                return;
            }

            try
            {
                log.Info().Msg(Logging.Constants.SyncStarted);

                try
                {
                    await Greetings.Greet(conn!, pid, id, (byte)AlertState.Finished);
                }
                catch (Exception ex)
                {
                    Log.Error().Str("where", "AlertHandler.SendFinished.Greet").Msg(ex.Message);
                    return;
                }

                var err = await Rmc.SendFinished(id, conn!);
                if (err != null)
                {
                    log.Error().Str("where", "AlertHandler.SendFinished.SendFinished").Msg(err.Message);
                    return;
                }

                try
                {
                    conn.Flush();
                }
                catch (Exception ex)
                {
                    log.Error().Str("where", "AlertHandler.SendFinished.Flush").Msg(ex.Message);
                }
            }
            finally
            {
                await conn.Close();
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

        public async Task HandleCommitmentRequest(Conn conn, Logger log)
        {
            Hash requested = new Hash(new byte[32]);
            
            try
            {
                await conn.Read(requested);
            }
            catch (Exception ex)
            {
                log.Error().Str("where", "AlertHandler.HandleCommitmentRequest.Read").Msg(ex.Message);
                return;
            }

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
                if (IsForker(unit.Creator()))
                {
                    log.Error().Str("where", "AlertHandler.HandleCommitmentRequest.GetByHash").Msg("alerter was unaware a fork was present");

                    string worf = "Write";
                    try
                    {
                        await conn.Write(new byte[] { 1 });
                        worf = "Flush";
                        conn.Flush();
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
                await conn.Write(new byte[] { 0 });
                await conn.Write(comm.Marshal());
                f = "WriteUnit";
                await conn.Write(EncodeUtil.EncodeUnit(null!));
                f = "Flush";
                conn.Flush();
                f = "SendFinished";
                var exc = await Rmc.SendFinished(comm.RmcID(), conn);
                if (exc != null) throw exc;
                f = "Flush`2";
                conn.Flush();
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

            (var conn, var err) = await Netserv.TryDial(pid);
            if (err != null)
            {
                return err;
            }

            log.Info().Msg(Logging.Constants.SyncStarted);

            try
            {
                try
                {
                    await Greetings.Greet(conn, MyPid, 0, (byte)AlertState.Request);
                }
                catch (Exception ex)
                {
                    log.Error().Str("where", "AlertHandler.RequestCommitment.Greet").Msg(ex.Message);
                    return ex;
                }

                (_, err) = await conn.TryWrite(pu.Hash());
                if (err != null)
                {
                    log.Error().Str("where", "AlertHandler.RequestCommitment.Write").Msg(err.Message);
                    return err;
                }

                err = conn.TryFlush();
                if (err != null)
                {
                    log.Error().Str("where", "AlertHandler.RequestCommitment.Flush").Msg(err.Message);
                    return err;
                }

                var buf = new byte[1];
                (_, err) = await conn.TryRead(buf);
                if (err != null)
                {
                    log.Error().Str("where", "alertHandler.RequestCommitment.Read").Msg(err.Message);
                    return err;
                }

                if (buf[0] == 1)
                {
                    await SendFinished(pu.Creator(), pid);
                    return new Exception("peer was unaware of forker");
                }

                (var comms, err) = await ForkingUtil.AcquireCommitments(conn);
                if (err != null)
                {
                    log.Error().Str("where", "alertHandler.RequestCommitment.AcquireCommitments").Msg(err.Message);
                    return err;
                }

                (_, var raiser, _, _) = DecodeAlertID(comms[0].RmcID(), 0);
                (var data, err) = await Rmc.AcceptFinished(comms[0].RmcID(), raiser, conn);
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
            finally
            {
                await conn.Close();
            }
        }

        public async Task AcceptAlert(ulong id, ushort pid, Conn conn, Logger log)
        {
            (var forker, _, var epochID, var err) = DecodeAlertID(id, pid);
            if (err != null)
            {
                log.Error().Str("where", "AlertHandler.AcceptAlert.DecodeAlertID").Msg(err.Message);
                return;
            }

            (var data, err) = await Rmc.AcceptData(id, pid, conn);
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
            err = await MaybeSign(id, conn);
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

        public async Task<Exception?> MaybeSign(ulong id, Conn conn)
        {
            var err = await Rmc.SendSignature(id, conn);
            if (err != null)
            {
                return err;
            }

            try
            {
                conn.Flush();
                return null;
            }
            catch (Exception e)
            {
                return e;
            }
        }

        public async Task AcceptProof(ulong id, Conn conn, Logger log)
        {
            var err = await Rmc.AcceptProof(id, conn);
            if (err != null)
            {
                log.Error().Str("where", "AlertHandler.AcceptProof.AcceptProof").Msg(err.Message);
            }
        }

        // FIXME: force synchronous of HandleForkerUnit or something else?
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

            var maxes = (await Orderer.MaxUnits(u.EpochID())).Get(creator);
            if (maxes == null || maxes.Count == 0)
            {
                return false;
            }

            // we can only have one max because the creator is not a forker yet.
            var max = maxes.First();
            if (max.Height() >=u.Height())
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

                // FIXME: check if this needs to be pinned to this routine or is fine running as its own task
                _ = Task.Run(async () => await RaiseAlert(proof));
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

                _ = Task.Run(async () =>
                {
                    wg.Add(1);
                    gathering.Add(1);
                    await SendAlert(data, id, pid, gathering, wg);
                });
                await wg.WaitAsync();

                var comm = proof.ExtractCommitment(id);
                Commitments.Add(comm, MyPid, proof.ForkerID());
            }
        }

        public async Task<Exception?> AttemptGather(Conn conn, byte[] data, ulong id, ushort pid)
        {
            try
            {
                await Greetings.Greet(conn, MyPid, id, (byte)AlertState.Alert);

                var err = await Rmc.SendData(id, data, conn);
                if (err != null) return err;
                
                conn.Flush();
                (_, err) = await Rmc.AcceptSignature(id, pid, conn);
                if (err != null) return err;

                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
            finally
            {
                await conn.Close();
            }
        }

        public async Task<Exception?> AttemptProve(Conn conn, ulong id)
        {
            try
            {
                await Greetings.Greet(conn, MyPid, id, (byte)AlertState.Proving);

                var err = await Rmc.SendProof(id, conn);
                if (err != null) return err;

                conn.Flush();
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
            finally
            {
                await conn.Close();
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

                while (Rmc.Status(id) != Status.Finished)
                {
                    Conn conn;
                    try
                    {
                        conn = await Netserv.Dial(pid);
                    }
                    catch
                    {
                        continue;
                    }

                    log.Info().Msg(Logging.Constants.SyncStarted);
                    var err = await AttemptGather(conn, data, id, pid);
                    if (err != null)
                    {
                        log.Error().Str("where", "AlertHandler.SendAlert.AttemptGather").Msg(err.Message);
                    }
                    else
                    {
                        log.Info().Msg(Logging.Constants.SyncCompleted);
                        success = true;
                        break;
                    }
                }

                gathering.Done();
                await gathering.WaitAsync();

                if (success)
                {
                    Conn conn;
                    try
                    {
                        conn = await Netserv.Dial(pid);
                    }
                    catch
                    {
                        return;
                    }

                    var err = await AttemptProve(conn, id);
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
