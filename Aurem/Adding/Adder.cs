using Aurem.Common;
using Aurem.Logging;
using Aurem.Model;
using Aurem.Model.Exceptions;
using AuremCore.FastLogger;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Adding
{
    /// <summary>
    /// Adder is a buffer zone where preunits wait to be added to the DAG.
    /// A preunit with missing parents is waiting until all parents are available.
    /// Then, it's considered "ready" and added to the per-pid queue, where it will get picked by the worker.
    /// Adding a unit consists of:
    /// <list type="bullet">
    /// <item>DecodeParents</item>
    /// <item>BuildUnit</item>
    /// <item>Check</item>
    /// <item>Insert</item>
    /// </list>
    /// </summary>
    public class Adder : IAdder
    {
        public IDag Dag;
        public IAlerter Alerter;
        public Config.Config Conf;
        public ISyncer Syncer;
        public Queue<WaitingPreunit>[] Ready;
        public ConcurrentDictionary<Hash, WaitingPreunit> Waiting;
        public ConcurrentDictionary<ulong, WaitingPreunit> WaitingByID;
        public ConcurrentDictionary<ulong, MissingPreunit> Missing;
        public bool Finished;
        private readonly object _finishedLock = new object();
        public Logger Log;
        public ulong WaitGroup;
        public SemaphoreSlim Mx;
        
        public Adder(IDag dag, Config.Config conf, ISyncer syncer, IAlerter alert, Logger log)
        {
            Dag = dag;
            Alerter = alert;
            Conf = conf;
            Syncer = syncer;
            Ready = new Queue<WaitingPreunit>[dag.NProc()];
            Waiting = new(new Hash.HashEqualityComparer());
            WaitingByID = new();
            Missing = new();
            Finished = false;
            WaitGroup = 0;
            Mx = new(1, 1);
            Log = log.With().Val(Constants.Service, Constants.AdderService).Logger();
            for (int i = 0; i < Ready.Length; i++)
            {
                if (i == conf.Pid) continue;
                Ready[i] = new(conf.EpochLength);
                var ch = Ready[i];
                _ = Task.Run(async () =>
                {
                    Interlocked.Increment(ref WaitGroup);

                    try
                    {
                        while (true)
                        {
                            lock (_finishedLock) if (Finished) return;

                            var success = ch.TryDequeue(out var wp);
                            if (success)
                            {
                                await HandleReady(wp);
                            }

                            await Task.Delay(50);
                        }
                    }
                    finally
                    {
                        Interlocked.Decrement(ref WaitGroup);
                    }
                });
            }

            Log.Info().Msg(Constants.ServiceStarted);
        }

        public async Task Close()
        {
            lock (_finishedLock) Finished = true;
            while (Interlocked.Read(ref WaitGroup) > 0)
            {
                await Task.Delay(100);
            }

            Log.Info().Msg(Constants.ServiceStopped);
        }

        /// <summary>
        /// Checks basic correctness of a list of units and then adds correct ones to the buffer zone.
        /// Returned list can have the following errors:
        /// <list type="bullet">
        /// <item><see cref="DataException"/> - if creator or signature are incorrect.</item>
        /// <item><see cref="DuplicateUnitException"/>, <see cref="DuplicatePreunitException"/> - if such a unit is already in the adder or DAG.</item>
        /// <item><see cref="UnknownParentsException"/> - in this case, the preunit is normally added and processed; this is returned only for log purposes.</item>
        /// </list>
        /// </summary>
        /// <param name="source"></param>
        /// <param name="preunits"></param>
        /// <returns></returns>
        public async Task<List<Exception?>?> AddPreunits(ushort source, params IPreunit[] preunits)
        {
            try
            {
                Log.Debug().Val(Constants.Size, preunits.Length).Val(Constants.PID, source).Msg(Constants.AddPreunits);

                List<Exception?>? errors = null;
                var getErrors = () =>
                {
                    if (errors == null || errors.Count == 0) errors = Enumerable.Repeat<Exception?>(null, preunits.Length).ToList();
                    return errors;
                };

                var hashes = preunits.Select(x => x.Hash()).ToArray();
                var alreadyInDag = Dag.GetUnits(hashes);

                var failed = Enumerable.Repeat(false, preunits.Length).ToArray();
                foreach ((var i, var pu) in preunits.Index())
                {
                    if (alreadyInDag[i] == null)
                    {
                        var err = CheckCorrectness(pu);
                        if (err != null)
                        {
                            getErrors()[i] = err;
                            failed[i] = true;
                        }
                    }
                    else
                    {
                        getErrors()[i] = new DuplicateUnitException(alreadyInDag[i]);
                        failed[i] = true;
                    }
                }

                await Mx.WaitAsync();

                try
                {
                    foreach ((var i, var pu) in preunits.Index())
                    {
                        if (!failed[i])
                        {
                            getErrors()[i] = await AddToWaiting(pu, source);
                        }
                    }

                    return errors;
                }
                finally
                {
                    Mx.Release();
                }
            }
            catch (Exception ex)
            {
                //await Console.Out.WriteLineAsync($"AddPreunits failed bc {ex.Message}");
                throw ex;
            }
        }

        /// <summary>
        /// Takes a WaitingPreunit that is ready and adds it to the DAG.
        /// </summary>
        /// <param name="wp"></param>
        /// <returns></returns>
        public async Task HandleReady(WaitingPreunit wp)
        {
            try
            {
                var log = Log.With().Val(Constants.Height, wp.Pu.Height()).Val(Constants.Creator, wp.Pu.Creator()).Val(Constants.PID, wp.Source).Logger();
                log.Debug().Msg(Constants.PreunitReady);

                // 1. decode parents
                List<IUnit> parents;
                try
                {
                    parents = Dag.DecodeParents(wp.Pu);
                }
                catch (Exception _e)
                {
                    if (_e is AmbiguousParentsException e)
                    {
                        parents = new List<IUnit>(e.Units.Length);
                        foreach (var us in e.Units)
                        {
                            IUnit parent;
                            Exception? caught = null;
                            (parent, caught) = await Alerter.Disambiguate(us, wp.Pu);


                            caught = await Alerter.ResolveMissingCommitment(caught, wp.Pu, wp.Source);
                            if (caught != null) break;
                        }
                    }

                    log.Error().Str("where", "DecodeParents").Msg(_e.Message);
                    wp.Failed = true;
                    return;
                }

                if (!Hash.CombineHashes(IUnit.ToHashes(parents)).Equals(wp.Pu.View().ControlHash))
                {
                    wp.Failed = true;
                    Log.Warn().Hex(Constants.ControlHash, wp.Pu.View().ControlHash.Data).Val(Constants.PID, wp.Source).Val(Constants.Height, wp.Pu.View().Heights).Msg(Constants.InvalidControlHash);
                    await HandleInvalidControlHash(wp.Source, wp.Pu, parents);
                    return;
                }

                // 2. build unit
                var freeUnit = Dag.BuildUnit(wp.Pu, parents);

                // 3. check
                await Alerter.Lock(freeUnit.Creator());
                try
                {
                    var err = Dag.Check(freeUnit);
                    err = await Alerter.ResolveMissingCommitment(err, freeUnit, wp.Source);
                    if (err != null)
                    {
                        log.Error().Str("where", "Check").Msg(err.Message);
                        wp.Failed = true;
                        return;
                    }

                    // 4. Insert
                    Dag.Insert(freeUnit);

                    log.Debug().Val(Constants.Level, freeUnit.Level()).Msg(Constants.UnitAdded);
                }
                finally
                {
                    Alerter.Unlock(freeUnit.Creator());
                }
            }
            finally 
            {
                await Remove(wp);
            }
        }

        public async Task HandleInvalidControlHash(ushort sourcePID, IPreunit witness, IList<IUnit> candidateParents)
        {
            var heights = witness.View().Heights;
            var ids = new ulong[heights.Length];
            for (int i = 0; i < heights.Length; i++)
            {
                ids[i] = IPreunit.ID(heights[i], (ushort)i, witness.EpochID());
            }

            // this should trigger download of all parents including some that may witness a fork, and start an alert as they are added.
            await Syncer.RequestFetch(sourcePID, ids);
        }

        /// <summary>
        /// Checks very basic correctness of the given preunit (creator and signature).
        /// </summary>
        /// <param name="pu"></param>
        /// <returns></returns>
        public Exception? CheckCorrectness(IPreunit pu)
        {
            if (pu.Creator() >= Dag.NProc()) return new DataException("invalid creator");
            if (pu.EpochID() != Dag.EpochID()) return new DataException($"invalid EpochID - expected {Dag.EpochID()} but received {pu.EpochID()} instead");
            if (!Conf.PublicKeys[pu.Creator()].Verify(pu)) return new DataException("invalid signature");

            return null;
        }

        /// <summary>
        /// Checks if a WaitingPreunit is ready (i.e. no waiting or missing parents). If so, send the preunit to the channel corresponding to its dedicated worker.
        /// </summary>
        /// <param name="wp"></param>
        /// <returns></returns>
        public void SendIfReady(WaitingPreunit wp)
        {
            if (wp.WaitingParents == 0 && wp.MissingParents == 0)
            {
                lock (_finishedLock) if (Finished) return;
                lock (Ready[wp.Pu.Creator()])
                {
                    Ready[wp.Pu.Creator()].Enqueue(wp);
                }
            }
        }

        /// <summary>
        /// Registers the given WaitingPreunit as needing an unknown unit with the given ID.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="wp"></param>
        public void RegisterMissing(ulong id, WaitingPreunit wp)
        {
            if (Missing.ContainsKey(id))
            {
                Missing[id].AddNeeding(wp);
            }
            else
            {
                Missing[id] = new MissingPreunit();
            }
        }

        /// <summary>
        /// FetchMissing is called on a freshly created WaitingPreunit that has some missing parents. 
        /// Sends a signal to trigger fetch or gossip.
        /// </summary>
        /// <param name="wp"></param>
        /// <param name="maxHeights"></param>
        public async Task FetchMissing(WaitingPreunit wp, IList<int> maxHeights)
        {
            var epoch = wp.Pu.EpochID();
            var toRequest = new List<ulong>(8);
            MissingPreunit mp;
            var now = DateTime.Now;

            foreach ((var creator, var height) in wp.Pu.View().Heights.Index())
            {
                for (int h = height;  h > (creator >= maxHeights.Count ? -1 : maxHeights[creator]); h--)
                {
                    var id = IPreunit.ID(h, (ushort)creator, epoch);
                    if (!WaitingByID.ContainsKey(id))
                    {
                        if (Missing.ContainsKey(id))
                        {
                            mp = new MissingPreunit();
                            Missing[id] = mp;
                        }
                        else
                        {
                            mp = Missing[id];
                        }

                        if (now.Subtract(mp.Requested) > Conf.FetchInterval)
                        {
                            toRequest.Add(id);
                            mp.Requested = now;
                        }
                    }
                }

                if (toRequest.Count > Conf.GossipAbove)
                {
                    await Syncer.RequestGossip(wp.Source);
                    return;
                }
            }

            if (toRequest.Count > 0)
            {
                await Syncer.RequestFetch(wp.Source, toRequest.ToArray());
            }
        }

        /// <summary>
        /// Sets the children attribute of a newly created WaitingPreunit, depending on if it was missing.
        /// </summary>
        /// <param name="wp"></param>
        public void CheckIfMissing(WaitingPreunit wp)
        {
            var success = Missing.TryGetValue(wp.Id, out var mp);
            if (success)
            {
                wp.Children = mp.NeededBy;
                foreach (var ch in wp.Children)
                {
                    ch.MissingParents--;
                    ch.WaitingParents++;
                }

                Missing.Remove(wp.Id, out _);
            }
            else
            {
                wp.Children = new(8);
            }
        }

        /// <summary>
        /// Finds out which parents of a newly created WaitingPreunit are either (1) in the DAG, (2) are waiting, and (3) are missing.
        /// Sets values of WaitingParents and MissingParents accordingly. Additionally returns maximal heights of the DAG.
        /// </summary>
        /// <param name="wp"></param>
        /// <returns></returns>
        public int[] CheckParents(WaitingPreunit wp)
        {
            var epoch = wp.Pu.EpochID();
            var maxHeights = DagInfo.MaxView(Dag).Heights;

            foreach ((var creator, var height) in wp.Pu.View().Heights.Index())
            {
                if (height > (creator >= maxHeights.Length ? -1 : maxHeights[creator]))
                {
                    var parentID = IPreunit.ID(height, (ushort)creator, epoch);
                    var success = WaitingByID.TryGetValue(parentID, out var par);
                    if (success)
                    {
                        wp.WaitingParents++;
                        par.Children.Add(wp);
                    }
                    else
                    {
                        wp.MissingParents++;
                        RegisterMissing(parentID, wp);
                    }
                }
            }

            return maxHeights;
        }

        /// <summary>
        /// Removes WaitingPreunit from the buffer zone and notifies its children.
        /// </summary>
        /// <param name="wp"></param>
        /// <returns></returns>
        public async Task Remove(WaitingPreunit wp)
        {
            await Mx.WaitAsync();

            try
            {
                if (wp.Failed)
                {
                    RemoveFailed(wp);
                }
                else
                {
                    Waiting.Remove(wp.Pu.Hash(), out _);
                    WaitingByID.Remove(wp.Id, out _);
                    foreach (var ch in wp.Children)
                    {
                        ch.WaitingParents--;
                        SendIfReady(ch);
                    }
                }
            }
            finally
            {
                Mx.Release();
            }
        }

        /// <summary>
        /// Removes a ready preunit from the buffer zone which we failed to add, together with all its descendants.
        /// </summary>
        /// <param name="wp"></param>
        public void RemoveFailed(WaitingPreunit wp)
        {
            Waiting.Remove(wp.Pu.Hash(), out _);
            WaitingByID.Remove(wp.Id, out _);
            foreach (var ch in wp.Children)
            {
                RemoveFailed(ch);
            }
        }

        /// <summary>
        /// Adds a preunit as a WaitingPreunit to the buffer zone. Must be called under mutex.
        /// </summary>
        /// <param name="pu"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public async Task<Exception?> AddToWaiting(IPreunit pu, ushort source)
        {
            if (Waiting.ContainsKey(pu.Hash())) return new DuplicatePreunitException(Waiting[pu.Hash()].Pu);
            
            var u = Dag.GetUnit(pu.Hash());
            if (u != null) return new DuplicateUnitException(u);

            var id = pu.UnitID();
            var success = WaitingByID.TryGetValue(id, out var fork);
            if (success)
            {
                Log.Warn().Val(Constants.Height, pu.Height()).Val(Constants.Creator, pu.Creator()).Val(Constants.PID, source).Msg(Constants.ForkDetected);
                await Alerter.NewFork(pu, fork.Pu);
            }

            var wp = new WaitingPreunit { Pu = pu, Id = id, Source = source };
            Waiting[pu.Hash()] = wp;
            WaitingByID[id] = wp;
            var maxHeights = CheckParents(wp);
            CheckIfMissing(wp);

            if (wp.MissingParents > 0)
            {
                Log.Debug().Val(Constants.Height, wp.Pu.Height()).Val(Constants.Creator, wp.Pu.Creator()).Val(Constants.PID, source).Val(Constants.Size, wp.MissingParents).Msg(Constants.UnknownParents);
                await FetchMissing(wp, maxHeights);
                return new UnknownParentsException(wp.MissingParents);
            }
            
            SendIfReady(wp);
            return null;
        }
    }
}
