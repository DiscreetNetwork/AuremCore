using Aurem.Adding;
using Aurem.Creating;
using Aurem.Model;
using Aurem.Model.Exceptions;
using Aurem.Random;
using AuremCore.Core;
using AuremCore.FastLogger;
using Nito.AsyncEx;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Aurem.Ordering
{
    public class Orderer : IOrderer
    {
        public Config.Config Conf;
        public ISyncer Syncer;
        public IRandomSourceFactory Rsf;
        public IAlerter Alerter;
        public PreblockMaker ToPreblock { get; set; }
        public IDataSource Ds;
        public Creator Creator;
        public Epoch Current;
        public Epoch Previous;
        public Channel<IUnit> UnitBelt { get; set; } // Note: units on the unit belt do not have to appear in topological order
        public Channel<IUnit> LastTiming; // Used to pass the last timing unit of the epoch to the creator
        public Channel<List<IUnit>> OrderedUnits { get; set; }
        public AsyncReaderWriterLock Mx;
        public WaitGroup Wg;
        public Logger Log;

        private CancellationTokenSource TokenSourceTiming;
        private CancellationTokenSource TokenSourceOrdered;
        private CancellationTokenSource TokenSourceBelt;
        private CancellationTokenSource Source;

        public Orderer(Config.Config conf, IDataSource ds, PreblockMaker toPreblock, Logger log)
        {
            Conf = conf;
            ToPreblock = toPreblock;
            Ds = ds;
            UnitBelt = Channel.CreateBounded<IUnit>(conf.EpochLength*conf.NProc);
            LastTiming = Channel.CreateBounded<IUnit>(conf.NumberOfEpochs);
            OrderedUnits = Channel.CreateBounded<List<IUnit>>(conf.EpochLength);
            Log = log.With().Val(Logging.Constants.Service, Logging.Constants.OrderService).Logger();
            Mx = new AsyncReaderWriterLock();
            Wg = new WaitGroup();
            TokenSourceTiming = new();
            TokenSourceOrdered = new();
            TokenSourceBelt = new();
            Source = new();
        }

        public async Task Start(IRandomSourceFactory rsf, ISyncer syncer, IAlerter alerter)
        {
            Rsf = rsf;
            Syncer = syncer;
            Alerter = alerter;

            var send = async (IUnit u) =>
            {
                if (u.Level() > 0)
                {
                    Log.Debug().Val(Logging.Constants.Level, u.Level()).Val(Logging.Constants.Height, u.Height()).Val(Logging.Constants.Creator, u.Creator()).Msg("attempting to send unit");
                }
                await Insert(u);
                if (u.Level() > 0)
                {
                    Log.Debug().Val(Logging.Constants.Level, u.Level()).Val(Logging.Constants.Height, u.Height()).Val(Logging.Constants.Creator, u.Creator()).Msg("sending unit has been inserted");
                }
                await Syncer.Multicast(u);
            };

            var epochProofBuilder = EpochProofBuilder.Factory(Conf, Log);
            Creator = new Creator(Conf, Ds, send, RsData, epochProofBuilder, Log.With().Val(Logging.Constants.Service, Logging.Constants.CreatorService).Logger());

            await NewEpoch(0);
            await Syncer.Start();
            Alerter.Start();

            Wg.Add(1);
            _ = Task.Run(async () =>
            {
                try
                {
                    await Creator.CreateUnits(UnitBelt, LastTiming, alerter, TokenSourceTiming.Token);
                }
                finally
                {
                    Wg.Done();
                }
            });

            Wg.Add(1);
            _ = Task.Run(async () =>
            {
                try
                {
                    await HandleTimingRounds();
                }
                finally
                {
                    Wg.Done();
                }
            });

            var ticker = new PeriodicTimer(Conf.GossipInterval);
            _ = Task.Run(async () =>
            {
                var rng = new System.Random();
                try
                {
                    while (await ticker.WaitForNextTickAsync(Source.Token))
                    {
                        if (Source.Token.IsCancellationRequested)
                        {
                            break;
                        }

                        var res = false;
                        while (!res)
                        {
                            var pidToCall = (ushort)rng.Next(Conf.NProc - 1);
                            if (pidToCall >= Conf.Pid)
                            {
                                pidToCall++;
                            }
                            res = await Syncer.RequestGossip(pidToCall);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    if (Source.Token.IsCancellationRequested)
                    {
                        ticker.Dispose();
                    }
                }
            });

            Log.Log().Msg(Logging.Constants.ServiceStarted);
        }

        public async Task Stop()
        {
            await Alerter.Stop();
            await Syncer.Stop();

            if (Previous != null) await Previous.Close();
            if (Current != null) await Current.Close();

            TokenSourceOrdered.Cancel();
            TokenSourceBelt.Cancel();
            Source.Cancel();
            OrderedUnits.Writer.Complete();
            UnitBelt.Writer.Complete();
            await Wg.WaitAsync();

            Log.Log().Msg(Logging.Constants.ServiceStopped);
        }

        public async Task HandleTimingRounds()
        {
            try
            {
                uint current = 0;
                await foreach (var round in OrderedUnits.Reader.ReadAllAsync())
                {
                    var timingUnit = round!.Last();
                    var epoch = timingUnit.EpochID();
                    if (timingUnit.Level() == Conf.LastLevel)
                    {
                        await LastTiming.Writer.WriteAsync(timingUnit);
                        await FinishEpoch(epoch);

                        if ((int)epoch == Conf.NumberOfEpochs - 1)
                        {
                            Source.Cancel();
                        }
                    }

                    if (epoch >= current && timingUnit.Level() <= Conf.LastLevel)
                    {
                        // consider data to be finalized here.
                        await ToPreblock(round!);
                        Log.Info().Val(Logging.Constants.Level, timingUnit.Level()).Val(Logging.Constants.Epoch, epoch).Msg(Logging.Constants.PreblockProduced);
                    }

                    current = epoch;
                }
            }
            finally
            {
                LastTiming.Writer.Complete();
                TokenSourceTiming.Cancel();
            }
        }

        public async Task<List<Exception>?> AddPreunits(ushort source, params IPreunit[] preunits)
        {
            if (preunits.Length > 1)
            {

            }
            var errorsSize = preunits.Length;
            Exception[] errors = null!;
            var getErrors = () =>
            {
                if (errors == null)
                {
                    errors = new Exception[errorsSize];
                }

                return errors;
            };

            var processed = 0;
            while (preunits.Length > 0)
            {
                var epoch = preunits[0].EpochID();
                var end = 0;
                while (end < preunits.Length && preunits[end].EpochID() == epoch)
                {
                    end++;
                }

                var ep = await RetrieveEpoch(preunits[0], source);
                if (ep != null)
                {
                    var _errs = await ep.Adder.AddPreunits(source, preunits[0..end]);
                    var errs = _errs?.ToArray() ?? Array.Empty<Exception>();
                    // TODO: double check this math
                    Array.Copy(errs, 0, getErrors(), processed, errs.Length + processed > errorsSize ? errorsSize - processed : errs.Length);
                }

                preunits = preunits[end..^0];
                processed += end;
            }

            return errors?.ToList();
        }

        public async Task<IUnit[]> UnitsByID(params ulong[] ids)
        {
            var result = Enumerable.Empty<IUnit>();

            var locker = await Mx.ReaderLockAsync();
            try
            {
                foreach (var id in ids)
                {
                    (_, _, var epoch) = IPreunit.DecodeID(id);
                    (var ep, _) = await GetEpoch(epoch);
                    if (ep != null)
                    {
                        result = result.Concat(ep.Dag.GetByID(id));
                    }
                }

                return result.ToArray();
            }
            finally
            {
                locker.Dispose();
            }
        }

        public async Task<IUnit[]> UnitsByHash(params Hash[] hashes)
        {
            IUnit[] result;

            var locker = await Mx.ReaderLockAsync();
            try
            {
                if (Current != null)
                {
                    result = Current.Dag.GetUnits(hashes);
                }
                else
                {
                    result = new IUnit[hashes.Length];
                }
                if (Previous != null)
                {
                    for (int i = 0; i < result.Length; i++)
                    {
                        if (result[i] == null)
                        {
                            result[i] = Previous.Dag.GetUnit(hashes[i]);
                        }
                    }
                }

                return result;
            }
            finally
            {
                locker.Dispose();
            }
        }

        public async Task<ISlottedUnits> MaxUnits(uint epoch)
        {
            (var ep, _) = await GetEpoch(epoch);
            if (ep != null) return ep.Dag.MaximalUnitsPerProcess();

            return null!;
        }

        public async Task<DagInfo[]> GetInfo()
        {
            var locker = await Mx.ReaderLockAsync();

            try
            {
                var result = new DagInfo[2];
                if (Previous != null && !await Previous.WantsMoreUnits())
                {
                    result[0] = DagInfo.MaxView(Previous.Dag);
                }
                if (Current != null && !await Current.WantsMoreUnits())
                {
                    result[1] = DagInfo.MaxView(Current.Dag);
                }

                return result;
            }
            finally
            {
                locker.Dispose();
            }
        }

        public async Task<byte[]> RsData(int level, IList<IUnit> parents, uint epoch)
        {
            byte[] result = null!;
            Exception? err;

            if (level == 0)
            {
                (result, err) = Rsf.DealingData(epoch);
            }
            else
            {
                (var ep, _) = await GetEpoch(epoch);
                if (ep != null)
                {
                    (result, err) = ep.Rs.DataToInclude(parents.ToList(), level);
                    if (ep.Rs is Coin)
                    {

                    }
                }
                else
                {
                    err = new DataException("unknown epoch");
                }
            }

            if (err != null)
            {
                Log.Error().Str("where", "Orderer.RsData").Msg(err.Message);
                return Array.Empty<byte>();
            }

            return result;
        }

        public async Task Insert(IUnit unit)
        {
            if (unit.Creator() != Conf.Pid)
            {
                Log.Warn().Val(Logging.Constants.Creator, unit.Creator()).Msg(Logging.Constants.InvalidCreator);
                return;
            }

            (var ep, var newer) = await GetEpoch(unit.EpochID());
            if (newer)
            {
                ep = await NewEpoch(unit.EpochID());
            }

            if (ep != null)
            {
                ep.Dag.Insert(unit);
                Log.Info().Val(Logging.Constants.Creator, unit.Creator()).Val(Logging.Constants.Epoch, unit.EpochID()).Val(Logging.Constants.Height, unit.Height()).Val(Logging.Constants.Level, unit.Level()).Msg(Logging.Constants.UnitAdded);
            }
            else
            {
                Log.Warn().Val(Logging.Constants.Epoch, unit.EpochID()).Val(Logging.Constants.Height, unit.Height()).Val(Logging.Constants.Level, unit.Level()).Msg(Logging.Constants.UnableToRetrieveEpoch);
            }
        }

        public async Task<List<IUnit>> Delta(DagInfo[] info)
        {
            if (info == null || info.Length != 2) throw new Exception("critical error!");

            var locker = await Mx.ReaderLockAsync();

            try
            {
                List<IUnit> result = new List<IUnit>();
                var deltaResolver = (DagInfo dagInfo) =>
                {
                    if (dagInfo == null) return;

                    if (Previous != null && dagInfo.Epoch == Previous.EpochID)
                    {
                        result.AddRange(Previous.UnitsAbove(dagInfo.Heights));
                    }
                    if (Current != null && dagInfo.Epoch == Current.EpochID)
                    {
                        result.AddRange(Current.UnitsAbove(dagInfo.Heights));
                    }
                };

                deltaResolver(info[0]);
                deltaResolver(info[1]);

                if (Current != null)
                {
                    if (info[0] != null && info[0].Epoch < Current.EpochID && info[1] != null && info[1].Epoch < Current.EpochID)
                    {
                        result.AddRange(Current.AllUnits());
                    }
                }

                return result;
            }
            finally
            {
                locker.Dispose();
            }
        }

        public async Task<Epoch> RetrieveEpoch(IPreunit pu, ushort source)
        {
            var epochID = pu.EpochID();
            (var epoch, var fromFuture) = await GetEpoch(epochID);
            if (fromFuture)
            {
                if (ProofUtil.EpochProof(pu, Conf.WTKey))
                {
                    epoch = await NewEpoch(epochID);
                }
                else
                {
                    await Syncer.RequestGossip(source);
                }
            }

            return epoch;
        }

        public async Task<(Epoch?, bool)> GetEpoch(uint epoch)
        {
            var locker = await Mx.ReaderLockAsync();
            
            try
            {
                if (Current == null || epoch > Current.EpochID)
                {
                    return (null, true);
                }
                if (epoch == Current.EpochID)
                {
                    return (Current, false);
                }
                if (epoch == Previous.EpochID)
                {
                    return (Previous, false);
                }

                return (null, false);
            }
            finally
            {
                locker.Dispose();
            }
        }

        public async Task<Epoch> NewEpoch(uint epoch)
        {
            var locker = await Mx.WriterLockAsync();

            try
            {
                if (Current == null || epoch > Current.EpochID)
                {
                    if (Previous != null)
                    {
                        await Previous.Close();
                    }

                    Previous = Current;
                    Current = new Epoch(epoch, Conf, Syncer, Rsf, Alerter, UnitBelt, OrderedUnits, Log);

                    return Current;
                }

                if (epoch == Current.EpochID)
                {
                    return Current;
                }

                if (epoch == Previous.EpochID)
                {
                    return Previous;
                }

                return null!;
            }
            finally
            {
                locker.Dispose();
            }
        }

        public async Task FinishEpoch(uint epoch)
        {
            (var ep, _) = await GetEpoch(epoch);
            ep?.NoMoreUnits();
        }
    }
}
