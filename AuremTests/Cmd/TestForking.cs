using Aurem.Adding;
using Aurem.Config;
using Aurem.Creating;
using Aurem.Crypto.Signing;
using Aurem.Dag;
using Aurem.Forking;
using Aurem.Model;
using Aurem.Model.Exceptions;
using Aurem.Random;
using Aurem.Syncing.Internals;
using Aurem.Units;
using AuremCore.Core;
using AuremCore.Core.Extensions;
using AuremCore.Crypto.P2P;
using AuremCore.FastLogger;
using AuremCore.Network;
using BN256Core;
using BN256Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AuremTests.Cmd
{
    public static class TestForking
    {
        private class NopOrderer : IOrderer
        {
            public NopOrderer() { }

            public Task<List<Exception>?> AddPreunits(ushort proc, params IPreunit[] preunits)
            {
                return Task.FromResult<List<Exception>?>(null);
            }

            public Task<IUnit[]> UnitsByID(params ulong[] ids)
            {
                return Task.FromResult(Array.Empty<IUnit>());
            }

            public Task<IUnit[]> UnitsByHash(params Hash[] hashes)
            {
                return Task.FromResult(Array.Empty<IUnit>());
            }

            public Task<ISlottedUnits> MaxUnits(uint epochId)
            {
                return Task.FromResult<ISlottedUnits>(null!);
            }

            public Task<DagInfo[]> GetInfo()
            {
                return Task.FromResult(new DagInfo[2]);
            }

            public Task<List<IUnit>> Delta(DagInfo[] info)
            {
                return Task.FromResult(new List<IUnit>());
            }

            public Task Start(IRandomSourceFactory randomSourceFactory, ISyncer syncer, IAlerter alerter)
            {
                return Task.CompletedTask;
            }

            public Task Stop()
            {
                return Task.CompletedTask;
            }
        }

        private class Orderer : NopOrderer, IOrderer
        {
            public IDag Dag;

            public void SetDag(IDag dag)
            {
                this.Dag = dag;
            }

            public new Task<ISlottedUnits> MaxUnits(uint epoch)
            {
                return Task.FromResult(Dag.MaximalUnitsPerProcess());
            }

            public new Task<IUnit[]> UnitsByHash(params Hash[] hashes)
            {
                return Task.FromResult(Dag.GetUnits(hashes));
            }

            public Orderer(IDag dag)
            {
                Dag = dag;
            }
        }

        private class NopRandomSource : IRandomSource
        {
            public byte[] RandomBytes(ushort pid, int level)
            {
                var ans = new byte[32];
                ans = ans.Concat(Encoding.ASCII.GetBytes($"{pid + level}"));
                return ans;
            }

            public (byte[], Exception?) DataToInclude(List<IUnit> units, int _)
            {
                return (Array.Empty<byte>(), null);
            }
        }

        private class NopAdder : IAdder
        {
            private IDag dag;

            public NopAdder(IDag dag)
            {
                this.dag = dag;
            }

            public Task<List<Exception?>?> AddPreunits(ushort id, params IPreunit[] preunits)
            {
                List<Exception?> errs = Enumerable.Repeat<Exception?>(null, preunits?.Length ?? 0).ToList();

                if (preunits == null) return Task.FromResult<List<Exception?>?>(null);

                for (int i = 0; i < preunits.Length; i++)
                {
                    var pu = preunits[i];
                    if (pu.EpochID() != dag.EpochID())
                    {
                        errs[i] = new Aurem.Model.Exceptions.DataException("wrong epoch");
                        continue;
                    }

                    var alreadyInDag = dag.GetUnit(pu.Hash());
                    if (alreadyInDag != null)
                    {
                        errs[i] = new Aurem.Model.Exceptions.DuplicateUnitException(alreadyInDag);
                        continue;
                    }

                    var parents = DelegateExtensions.InvokeAndCaptureException(dag.DecodeParents, pu, out var err);
                    if (err != null)
                    {
                        errs[i] = err;
                        continue;
                    }

                    var freeUnit = dag.BuildUnit(pu, parents);
                    err = dag.Check(freeUnit);
                    if (err != null)
                    {
                        errs[i] = err;
                        continue;
                    }

                    dag.Insert(freeUnit);
                }

                return Task.FromResult(errs.Any(x => x != null) ? errs : null);
            }

            public Task Close() => Task.CompletedTask;

            public static (IUnit, Exception?) AddUnit(IDag dag, IPreunit pu)
            {
                var err = new NopAdder(dag).AddPreunits(pu.Creator(), pu).GetAwaiter().GetResult();
                if (err != null && err.Count > 0 && err[0] != null)
                {
                    return (null!, err[0]);
                }

                return (dag.GetUnit(pu.Hash()), null);
            }
        }

        public static IPreunit ToPreunit(IUnit u)
        {
            var id = IPreunit.ID(u.Height(), u.Creator(), u.EpochID());
            return new Preunit(id, u.View(), u.Data(), u.RandomSourceData(), u.Signature());
        }

        public static IUnit[] RetrieveParentCandidates(IDag dag)
        {
            var parents = new List<IUnit>(dag.NProc());

            dag.MaximalUnitsPerProcess().Iterate(units =>
            {
                IUnit? candidate = null;
                if (units != null && units.Count > 0)
                {
                    candidate = units[0];
                }

                parents.Add(candidate!);
                return true;
            });

            Creator.MakeConsistent(parents);
            return parents.ToArray();
        }

        public static (IUnit, Exception?) NewUnit(IDag dag, ushort creator, IPrivateKey pk, IRandomSource rss, byte[] data)
        {
            var parents = RetrieveParentCandidates(dag);
            var lvl = IUnit.LevelFromParents(parents);
            (var rssData, var err) = rss.DataToInclude(parents.ToList(), lvl);
            if (err != null)
            {
                return (null!, err);
            }

            return (new FreeUnit(creator, dag.EpochID(), parents, lvl, data, rssData, pk), null);
        }

        private static bool NoExc(Exception? ex, bool printOnErr = true)
        {
            if (ex != null)
            {
                Console.WriteLine(printOnErr ? $"Expected error to not occur: {ex.Message}" : "Expected error to occur");
                return true;
            }

            return false;
        }

        private static bool MatchExc(Exception? ex, string msg)
        {
            if (ex != null)
            {
                return ex.Message == msg;
            }

            return false;
        }

        private static async Task<bool> EventuallySucceed(Task<Exception?> t, TimeSpan timeout, TimeSpan interval)
        {
            var tx = DateTime.Now.Add(timeout).Ticks;
            while (!t.IsCompleted && tx > DateTime.Now.Ticks)
            {
                await Task.Delay(interval);
            }

            if (tx <= DateTime.Now.Ticks)
            {
                return false;
            }

            if (t.IsFaulted)
            {
                return false;
            }

            if (t.Result != null)
            {
                return false;
            }

            
            return t.IsCompletedSuccessfully;
        }

        public static async Task TestAlert()
        {
            Console.WriteLine("Testing the Forking library...");

            ushort nProc = 0;
            IAlerter[] alerters = Array.Empty<IAlerter>();
            Orderer[] orderers = Array.Empty<Orderer>();
            IDag[] dags = Array.Empty<IDag>();
            IRandomSource[] rss = Array.Empty<IRandomSource>();
            Network[] netservs = Array.Empty<Network>();
            IPublicKey[] pubKeys = Array.Empty<IPublicKey>();
            IPrivateKey[] privKeys = Array.Empty<IPrivateKey>();
            VerificationKey[] verKeys = Array.Empty<VerificationKey>();
            SecretKey[] secKeys = Array.Empty<SecretKey>();
            WaitGroup wg = new WaitGroup();
            P2PSecretKey[] p2ppriv = Array.Empty<P2PSecretKey>();
            P2PPublicKey[] p2ppub = Array.Empty<P2PPublicKey>();
            ulong stop = 0;

            var timeout = TimeSpan.FromSeconds(2);

            Action beforeEach = () =>
            {
                nProc = 10;
                pubKeys = new IPublicKey[nProc];
                privKeys = new IPrivateKey[nProc];
                verKeys = new VerificationKey[nProc];
                secKeys = new SecretKey[nProc];
                p2ppriv = new P2PSecretKey[nProc];
                p2ppub = new P2PPublicKey[nProc];
                wg = new WaitGroup();

                for (int i = 0; i < nProc; i++)
                {
                    (pubKeys[i], privKeys[i]) = Keys.GenerateKeys();
                    (verKeys[i], secKeys[i]) = VerificationKey.GenerateKeys();
                    (p2ppub[i], p2ppriv[i]) = P2P.GenerateKeys();
                }

                alerters = new IAlerter[nProc];
                dags = new IDag[nProc];
                rss = new IRandomSource[nProc];
                // we no longer need local network, so instead use actual (local) conns
                //netservs = AuremCore.Tests.LocalServer.NewNetwork(nProc, timeout);
                netservs = new Network[nProc];
                var addrs = Enumerable.Range(0, nProc).Select(x => $"127.0.0.1:{25000 + x}").ToArray();
                orderers = new Orderer[nProc];
                Interlocked.Exchange(ref stop, 0);

                for (int i = 0; i < nProc; i++)
                {
                    var cnf = Config.Empty();
                    cnf.NProc = nProc;
                    cnf.Pid = (ushort)i;
                    cnf.RMCPublicKeys = verKeys.ToList();
                    cnf.RMCPrivateKey = secKeys[i];
                    cnf.PublicKeys = pubKeys.ToList();
                    cnf.P2PSecretKey = p2ppriv[i];
                    cnf.P2PPublicKeys = p2ppub.ToList();

                    orderers[i] = new Orderer(null);
                    netservs[i] = new Network(addrs[i], addrs, Logger.Nop(), TimeSpan.FromSeconds(5), cnf);

                    Exception? err = null;
                    alerters[i] = new AlertService(cnf, orderers[i], netservs[i], Logger.Nop());
                    dags[i] = new DAG(cnf, 0);
                    rss[i] = new NopRandomSource();
                    orderers[i].SetDag(dags[i]);

                    wg.Add(1);
                    var _i = i;
                    _ = Task.Run(async () =>
                    {
                        await KeepHandling((ushort)_i);
                    });
                }
            };

            Func<Task> afterEach = StopHandling;

            async Task KeepHandling(ushort pid)
            {
                alerters[pid].Start();
                //try
                //{
                //    while (Interlocked.Read(ref stop) == 0)
                //    {
                //        Conn? conn = null;
                //        try
                //        {
                //            conn = await netservs[pid].Listen();
                //        }
                //        catch
                //        {
                //            continue;
                //        }

                //        wg.Add(1);
                //        _ = Task.Run(async () => 
                //        {
                //            try
                //            {
                //                //await Console.Out.WriteLineAsync($"pid={pid} received handle incoming");
                //                _ = Task.Run(async () => await alerters[pid].HandleIncoming(conn!));
                //            }
                //            finally
                //            {
                //                wg.Done();
                //            }
                //        });
                //    }

                //    // clean up pending alerts;
                //    for (int i = 0; i < alerters.Length; i++)
                //    {
                //        Conn? conn = null;
                //        try
                //        {
                //            conn = await netservs[pid].Listen();
                //        }
                //        catch
                //        {
                //            return;
                //        }

                //        wg.Add(1);
                //        _ = Task.Run(() =>
                //        {
                //            try
                //            {
                //                Task.Run(async () => await alerters[pid].HandleIncoming(conn!));
                //            }
                //            finally
                //            {
                //                wg.Done();
                //            }
                //        });
                //    }
                //}
                //finally
                //{
                //    wg.Done();
                //}
            };

            async Task StopHandling()
            {
                Interlocked.Increment(ref stop);
                await wg.WaitAsync();
                await Task.WhenAll(alerters.Select(x => x.Stop()));
                // close network
                //await Console.Out.WriteLineAsync("StopHandling completed");
                //AuremCore.Tests.LocalServer.CloseNetwork(netservs);
            };

            Console.WriteLine("When the dags are empty...");
            {
                {
                    Console.WriteLine("Adds nonforking units without problems.");
                    beforeEach();

                    for (ushort i = 0; i < nProc; i++)
                    {
                        (var u, var err) = NewUnit(dags[i], i, privKeys[i], rss[i], Array.Empty<byte>());
                        if (NoExc(err)) return;

                        var pu = ToPreunit(u);
                        foreach (var dag in dags)
                        {
                            (_, err) = NopAdder.AddUnit(dag, pu);
                            if (NoExc(err)) return;
                        }
                    }

                    await afterEach();
                }

                {
                    Console.WriteLine("Does not add noncommitted forking units after an alert");
                    beforeEach();

                    ushort forker = 0;
                    (var u, var err) = NewUnit(dags[forker], forker, privKeys[forker], rss[forker], Array.Empty<byte>());
                    if (NoExc(err)) return;
                    var pu = ToPreunit(u);

                    (var uf, var errf) = NewUnit(dags[forker], forker, privKeys[forker], rss[forker], new byte[] {43});
                    if (NoExc(errf)) return;
                    var puf = ToPreunit(uf);

                    (_, err) = NopAdder.AddUnit(dags[1], pu);
                    if (NoExc(err)) return;
                    (_, err) = NopAdder.AddUnit(dags[1], puf);
                    if (!MatchExc(err, "MissingCommitment: missing commitment to fork")) return;

                    // we only know that at least 2f+1 processes received the alert, so at least they should be aware of the fork and react accordingly.
                    var ignorants = 0;
                    for (ushort j = 2; j < nProc; j++)
                    {
                        if (alerters[j].IsForker(forker))
                        {
                            (_, err) = NopAdder.AddUnit(dags[j], puf);
                            if (!MatchExc(err, "MissingCommitment: missing commitment to fork")) return;
                        }
                        else
                        {
                            ignorants++;
                        }
                    }

                    Console.WriteLine($"NUMIGNORANTS: {ignorants}");
                    if (ignorants > (nProc/3)-1)
                    {
                        await Console.Out.WriteLineAsync($"{nameof(ignorants)}={ignorants} was not less than or equal to {(nProc/3) - 1}");
                        return;
                    }

                    // add the correct unit everywhere to confirm that any alerts are done.
                    List<Task<bool>> _atcueTasks = new List<Task<bool>>();
                    List<ushort> _atcueIdxs = new List<ushort>();
                    for (ushort j = 2;j < nProc; j++)
                    {
                        (_, err) = NopAdder.AddUnit(dags[j], pu);
                        if (err != null)
                        {
                            _atcueIdxs.Add(j);
                            if (!MatchExc(err, "MissingCommitment: missing commitment to fork")) return;
                            _atcueTasks.Add(EventuallySucceed(alerters[j].RequestCommitment(pu, 1), TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(100)));
                        }
                    }

                    await Task.WhenAll(_atcueTasks.ToArray());
                    foreach (var success in _atcueTasks.Select(x => x.Result))
                    {
                        if (!success)
                        {
                            Console.WriteLine("Did not succeed");
                            return;
                        }
                    }

                    foreach (var j in _atcueIdxs)
                    {
                        (_, err) = NopAdder.AddUnit(dags[j], pu);
                        if (NoExc(err)) return;
                    }

                    await afterEach();
                }

                Console.WriteLine("And a forker creates a fork for every process");
                {
                    IPreunit[] pus = Array.Empty<IPreunit>();

                    var BeforeEachAndAForker = () =>
                    {
                        ushort forker = 0;
                        pus = new IPreunit[nProc];
                        for (ushort i = 1; i < nProc; i++)
                        {
                            (var u, var err) = NewUnit(dags[forker], forker, privKeys[forker], rss[forker], new byte[] { (byte)i });
                            if (NoExc(err)) return;
                            var pu = ToPreunit(u);

                            (_, err) = NopAdder.AddUnit(dags[i], pu);
                            if (NoExc(err)) return;
                            pus[i] = pu;
                        }

                        (var _u, var _err) = NewUnit(dags[forker], forker, privKeys[forker], rss[forker], new byte[] { 0 });
                        if (NoExc(_err)) return;
                        var _pu = ToPreunit(_u);
                        pus[0] = _pu;
                    };

                    Console.WriteLine("Adds committed forking units after acquiring commitments through alerts");
                    {
                        beforeEach();
                        BeforeEachAndAForker();

                        (_, var err) = NopAdder.AddUnit(dags[1], pus[0]);
                        if (!MatchExc(err, "MissingCommitment: missing commitment to fork")) return;
                        var failed = new List<ushort>();
                        for (ushort j = 2; j < nProc; j++)
                        {
                            (_, err) = NopAdder.AddUnit(dags[j], pus[1]);
                            if (err != null)
                            {
                                if (!MatchExc(err, "MissingCommitment: missing commitment to fork")) return;
                                failed.Add(j);
                            }
                        }

                        if (failed.Count > (nProc / 3) - 1)
                        {
                            await Console.Out.WriteLineAsync($"{nameof(failed.Count)}={failed.Count} was not less than or equal to {(nProc / 3) - 1}");
                            return;
                        }

                        // ensure any alerts are done by eventually adding the unit everywhere.
                        var _eaaadTasks = new List<Task<bool>>();
                        var _eaaadIdxs = new List<ushort>();
                        foreach (var j in failed)
                        {
                            (_, err) = NopAdder.AddUnit(dags[j], pus[1]);
                            if (err != null)
                            {
                                _eaaadIdxs.Add(j);
                                if (!MatchExc(err, "MissingCommitment: missing commitment to fork")) return;
                                _eaaadTasks.Add(EventuallySucceed(alerters[j].RequestCommitment(pus[1], 1), TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(100)));
                            }
                        }
                        await Task.WhenAll(_eaaadTasks.ToArray());
                        foreach (var success in _eaaadTasks.Select(x => x.Result))
                        {
                            if (!success)
                            {
                                Console.WriteLine("Did not succeed");
                                return;
                            }
                        }

                        foreach (var j in _eaaadIdxs)
                        {
                            (_, err) = NopAdder.AddUnit(dags[j], pus[1]);
                            if (NoExc(err)) return;
                        }

                        await afterEach();
                    }
                }
            }

            Console.WriteLine("When the dag contains all dealing units");
            {
                IPreunit[] dealing = Array.Empty<IPreunit>();

                var BeforeEachWhenTheDag = () =>
                {
                    dealing = new IPreunit[nProc];
                    for (ushort i = 1; i < nProc; i++)
                    {
                        (var u, var err) = NewUnit(dags[i], i, privKeys[i], rss[i], Array.Empty<byte>());
                        if (NoExc(err)) return;
                        var pu = ToPreunit(u);
                        dealing[i] = pu;
                        foreach (var dag in dags)
                        {
                            (_, err) = NopAdder.AddUnit(dag, pu);
                            if (NoExc(err))
                            {
                                return;
                            }
                        }
                    }
                };

                Console.WriteLine("And a forker creates two double unit forks");
                {
                    IPreunit dealingFork1 = null, dealingFork2 = null, childFork1 = null, childFork2 = null;
                    IDag forkHelpDag = null;

                    var BeforeEachAndAForker = () =>
                    {
                        Exception? err = null;
                        ushort forker = 0;
                        var forkerCnf = Config.Empty();
                        forkerCnf.NProc = nProc;
                        forkerCnf.Pid = forker;
                        forkerCnf.RMCPublicKeys = verKeys.ToList();
                        forkerCnf.RMCPrivateKey = secKeys[forker];
                        forkerCnf.PublicKeys = pubKeys.ToList();

                        forkHelpDag = new DAG(forkerCnf, 0);
                        for (ushort i = 1; i < nProc; i++)
                        {
                            (_, err) = NopAdder.AddUnit(forkHelpDag, dealing[i]);
                            if (NoExc(err)) return;
                        }

                        (var df1, err) = NewUnit(dags[forker], forker, privKeys[forker], rss[forker], Array.Empty<byte>());
                        if (NoExc(err)) return;
                        dealingFork1 = ToPreunit(df1);
                        (_, err) = NopAdder.AddUnit(dags[forker], dealingFork1);
                        if (NoExc(err)) return;

                        (var df2, err) = NewUnit(forkHelpDag, forker, privKeys[forker], rss[forker], new byte[] { 43 });
                        if (NoExc(err)) return;
                        dealingFork2 = ToPreunit(df2);
                        (_, err) = NopAdder.AddUnit(forkHelpDag, dealingFork2);
                        if (NoExc(err)) return;

                        (var cf1, err) = NewUnit(dags[forker], forker, privKeys[forker], rss[forker], Array.Empty<byte>());
                        if (NoExc(err)) return;
                        childFork1 = ToPreunit(cf1);
                        (_, err) = NopAdder.AddUnit(dags[forker], childFork1);
                        if (NoExc(err)) return;

                        (var cf2, err) = NewUnit(forkHelpDag, forker, privKeys[forker], rss[forker], new byte[] { 43 });
                        if (NoExc(err)) return;
                        childFork2 = ToPreunit(cf2);
                        (_, err) = NopAdder.AddUnit(forkHelpDag, childFork2);
                        if (NoExc(err)) return;

                        (_, err) = NopAdder.AddUnit(dags[1], dealingFork1);
                        if (NoExc(err)) return;
                        (_, err) = NopAdder.AddUnit(dags[1], childFork1);
                        if (NoExc(err)) return;
                        (_, err) = NopAdder.AddUnit(dags[2], dealingFork2);
                        if (NoExc(err)) return;
                        (_, err) = NopAdder.AddUnit(dags[2], childFork2);
                        if (NoExc(err)) return;
                    };

                    Console.WriteLine("Adds forks afer acquiring commitments explicitly");
                    {
                        beforeEach();
                        BeforeEachWhenTheDag();
                        BeforeEachAndAForker();


                        (_, var err) = NopAdder.AddUnit(dags[1], dealingFork2!);                        
                        if (!MatchExc(err, "MissingCommitment: missing commitment to fork")) return;
                        await Task.Delay(1000);
                        var succeed1 = await EventuallySucceed(alerters[1].RequestCommitment(dealingFork2!, 2), TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(100));
                        if (!succeed1)
                        {
                            Console.WriteLine("Did not succeed");
                            return;
                        }

                        (_, err) = NopAdder.AddUnit(dags[1], dealingFork2!);
                        if (NoExc(err)) return;
                        _ = DelegateExtensions.InvokeAndCaptureException(dags[1].DecodeParents, childFork2!, out err);
                        if (!NoExc(err, false)) return;
                        AmbiguousParentsException e;
                        if (err is AmbiguousParentsException _e)
                        {
                            e = _e;
                        }
                        else
                        {
                            await Console.Out.WriteLineAsync($"Expected err to be of type AmbiguousParentsException (was {err?.GetType()?.Name})");
                            return;
                        }

                        var parents = new List<IUnit>();
                        foreach (var us in e.Units)
                        {
                            (var parent, var err2) = await alerters[1].Disambiguate(us, childFork2!);
                            if (NoExc(err2)) return;
                            parents.Add(parent);
                        }

                        var fu = dags[1].BuildUnit(childFork2!, parents);
                        if (dags[1].Check(fu) != null)
                        {
                            await Console.Out.WriteLineAsync("Check did not succeed");
                            return;
                        }

                        // Add one of the dealing units everywhere to confirm that any alerts are done.
                        List<Task<bool>> ts = new List<Task<bool>>();
                        List<ushort> pidsToStillAdd = new List<ushort>();
                        for (ushort j = 2; j < nProc; j++)
                        {
                            (_, err) = NopAdder.AddUnit(dags[j], dealingFork1!);
                            if (err != null)
                            {
                                if (!MatchExc(err, "MissingCommitment: missing commitment to fork")) return;
                                ts.Add(EventuallySucceed(alerters[j].RequestCommitment(dealingFork1!, 1), TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(100)));
                                pidsToStillAdd.Add(j);
                            }
                        }

                        await Task.WhenAll(ts.ToArray());
                        foreach (var success in ts.Select(x => x.Result))
                        {
                            if (!success)
                            {
                                Console.WriteLine("Did not succeed");
                                return;
                            }
                        }

                        foreach (var j in pidsToStillAdd)
                        {
                            (_, err) = NopAdder.AddUnit(dags[j], dealingFork1!);
                            if (NoExc(err)) return;
                        }

                        await afterEach();
                    }

                    await Console.Out.WriteLineAsync("Adds a unit built on forks only after acquiring commitments explicitly");
                    {
                        beforeEach();
                        BeforeEachWhenTheDag();
                        BeforeEachAndAForker();

                        (var u2, var err) = NewUnit(dags[2], 2, privKeys[2], rss[2], Array.Empty<byte>());
                        if (NoExc(err)) return;
                        var unit2 = ToPreunit(u2);
                        (_, err) = NopAdder.AddUnit(dags[2], unit2);
                        if (NoExc(err)) return;
                        (_, err) = NopAdder.AddUnit(dags[1], dealingFork2!);
                        if (!MatchExc(err, "MissingCommitment: missing commitment to fork")) return;
                        var success1 = await EventuallySucceed(alerters[1].RequestCommitment(dealingFork2!, 2), TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(100));
                        if (!success1)
                        {
                            Console.WriteLine("Did not succeed");
                            return;
                        }

                        (_, err) = NopAdder.AddUnit(dags[1], dealingFork2!);
                        if (NoExc(err)) return;
                        _ = DelegateExtensions.InvokeAndCaptureException(dags[1].DecodeParents, childFork2!, out err);
                        if (!NoExc(err, false)) return;
                        AmbiguousParentsException e;
                        if (err is AmbiguousParentsException _e)
                        {
                            e = _e;
                        }
                        else
                        {
                            await Console.Out.WriteLineAsync($"Expected err to be of type AmbiguousParentsException (was {err?.GetType()?.Name})");
                            return;
                        }

                        var parents = new List<IUnit>();
                        foreach (var us in e.Units)
                        {
                            (var parent, var err2) = await alerters[1].Disambiguate(us, childFork2!);
                            if (NoExc(err2)) return;
                            parents.Add(parent);
                        }

                        var fu = dags[1].BuildUnit(childFork2!, parents);
                        if (dags[1].Check(fu) != null)
                        {
                            await Console.Out.WriteLineAsync("Check did not succeed");
                            return;
                        }

                        dags[1].Insert(fu);
                        _ = DelegateExtensions.InvokeAndCaptureException(dags[1].DecodeParents, unit2, out err);
                        if (!NoExc(err)) return;
                        if (err is AmbiguousParentsException _e2)
                        {
                            e = _e2;
                        }
                        else
                        {
                            await Console.Out.WriteLineAsync($"Expected err to be of type AmbiguousParentsException (was {err?.GetType()?.Name})");
                            return;
                        }

                        parents = new List<IUnit>();
                        foreach (var us in e.Units)
                        {
                            (var parent, var err2) = await alerters[1].Disambiguate(us, childFork2!);
                            if (NoExc(err2)) return;
                            parents.Add(parent);
                        }

                        fu = dags[1].BuildUnit(unit2, parents);
                        if (dags[1].Check(fu) != null)
                        {
                            await Console.Out.WriteLineAsync("Check did not succeed");
                            return;
                        }

                        // add one of the dealing units everywhere to confirm that any alerts are done.
                        List<Task<bool>> ts = new List<Task<bool>>();
                        List<ushort> pidsToStillAdd = new List<ushort>();
                        for (ushort j = 2; j < nProc; j++)
                        {
                            (_, err) = NopAdder.AddUnit(dags[j], dealingFork1!);
                            if (err != null)
                            {
                                if (!MatchExc(err, "MissingCommitment: missing commitment to fork")) return;
                                ts.Add(EventuallySucceed(alerters[j].RequestCommitment(dealingFork1!, 1), TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(100)));
                                pidsToStillAdd.Add(j);
                            }
                        }

                        await Task.WhenAll(ts.ToArray());
                        foreach (var success in ts.Select(x => x.Result))
                        {
                            if (!success)
                            {
                                Console.WriteLine("Did not succeed");
                                return;
                            }
                        }

                        foreach (var j in pidsToStillAdd)
                        {
                            (_, err) = NopAdder.AddUnit(dags[j], dealingFork1!);
                            if (NoExc(err)) return;
                        }

                        await afterEach();
                    }
                }
            }

            await Console.Out.WriteLineAsync("All tests have succeeded.");
        }
    }
}
