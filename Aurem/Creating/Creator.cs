using Aurem.Common;
using Aurem.Logging;
using Aurem.Model;
using Aurem.Serialize;
using Aurem.Units;
using AuremCore.Core;
using AuremCore.FastLogger;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Aurem.Creating
{
    /// <summary>
    /// A component responsible for producing new units. 
    /// It read units produced by other committee members from some external channel and stores the ones with the highest level as possible parents (candidates).
    /// Whenever there are enough parents to produce a unit on a new level, Creator collects data from its DataSource, and random source data using the prodived function.
    /// It then builds, signs, and sends (using a function given to the constructor) a new unit.
    /// </summary>
    public class Creator
    {
        public Config.Config Conf;
        public IDataSource Source;
        public Func<IUnit, Task> Send;
        public Func<int, IList<IUnit>, uint, Task<byte[]>> RSData;
        public uint Epoch;
        public bool EpochDone;
        public IUnit[] Candidates;
        public ushort Quorum;
        public int MaxLevel; // max level of units in candidates
        public ushort OnMaxLevel = 0; // number of candidates on MaxLevel
        public int Level = 0; // level of unit we could produce with current candidates
        public Dictionary<ushort, bool> Frozen;
        public SemaphoreSlim Mx;
        public Func<uint, IEpochProofBuilder> EpochProofFactory;
        public IEpochProofBuilder EpochProof;
        public Logger Log;

        /// <summary>
        /// Constructs a creator that uses the provided config, data source and logger. 
        /// Send function is called on each created unit. 
        /// rsData provides random source data for the given level, parents and epoch.
        /// It starts producing units for a given epoch.
        /// </summary>
        /// <param name="conf"></param>
        /// <param name="dataSource"></param>
        /// <param name="send"></param>
        /// <param name="rsData"></param>
        /// <param name="epochProofFactory"></param>
        /// <param name="log"></param>
        public Creator(Config.Config conf, IDataSource dataSource, Func<IUnit, Task> send, Func<int, IList<IUnit>, uint, Task<byte[]>> rsData, Func<uint, IEpochProofBuilder> epochProofFactory, uint epoch, Logger log)
        {
            Conf = conf;
            Source = dataSource;
            Send = send;
            RSData = rsData;
            EpochProofFactory = epochProofFactory;
            Log = log;
            Candidates = new IUnit[conf.NProc];
            MaxLevel = -1;
            Quorum = IDag.MinimalQuorum(conf.NProc);
            Frozen = new();
            Epoch = epoch;
            Log = log;
            Mx = new(1, 1);
        }

        /// <summary>
        /// Constructs a creator that uses the provided config, data source and logger. 
        /// Send function is called on each created unit. 
        /// rsData provides random source data for the given level, parents and epoch.
        /// </summary>
        /// <param name="conf"></param>
        /// <param name="dataSource"></param>
        /// <param name="send"></param>
        /// <param name="rsData"></param>
        /// <param name="epochProofFactory"></param>
        /// <param name="log"></param>
        public Creator(Config.Config conf, IDataSource dataSource, Func<IUnit, Task> send, Func<int, IList<IUnit>, uint, Task<byte[]>> rsData, Func<uint, IEpochProofBuilder> epochProofFactory, Logger log) : this(conf, dataSource, send, rsData, epochProofFactory, 0, log) { }
    
        /// <summary>
        /// Ensures that the set of parents follows the "parent consistency rule." Modifies the provided unit list in place.
        /// The parent consistency rule means that any unit's nth parent cannot be lower (in a level sense) than the nth parent of any other of that units parents.
        /// In other words, units seen from U directly as parents cannot be below the ones seen indirectly (parents of parents).
        /// </summary>
        /// <param name="parents"></param>
        public void MakeConsistent(IList<IUnit> parents)
        {
            for (int i = 0; i < parents.Count; i++)
            {
                for (int j = 0; j < parents.Count; j++)
                {
                    if (parents[j] == null) continue;

                    var u = parents[j].Parents().ToList()[i];
                    if (parents[i] == null || (u != null && u.Level() > parents[i].Level()))
                    {
                        parents[i] = u;
                    }
                }
            }
        }

        /// <summary>
        /// Switches the creator to a chosen epoch, resets candidates and shares a dealing with the provided data.
        /// </summary>
        /// <param name="epoch"></param>
        /// <param name="data"></param>
        public async Task NewEpoch(uint epoch, byte[] data)
        {
            Epoch = epoch;
            EpochDone = false;
            ResetEpoch();
            EpochProof = EpochProofFactory(epoch);
            Log.Log().Val(Constants.Epoch, epoch).Msg(Constants.NewEpoch);
            await CreateUnit(new IUnit[Conf.NProc], 0, data);
        }

        /// <summary>
        /// Creates a unit with the given parents, level,and data, Assumes provided parameters are consistent, meaning level == LevelFromParents(parents) and Epoch == parents[i].EpochID()
        /// </summary>
        /// <param name="parents"></param>
        /// <param name="level"></param>
        /// <param name="data"></param>
        public async Task CreateUnit(IUnit[] parents, int level, byte[] data)
        {
            var rsData = await RSData(level, parents, Epoch);
            var u = new FreeUnit(Conf.Pid, Epoch, parents, level, data, rsData, Conf.PrivateKey);
            Log.Info().Val(Constants.Epoch, u.EpochID()).Val(Constants.Height, u.Height()).Val(Constants.Level, level).Msg(Constants.UnitCreated);

            // FIXME remove
            if (data != null && data.Length > 0 && Conf.Pid == 0)
            {
                await Console.Out.WriteLineAsync($"PID={Conf.Pid} Epoch={u.EpochID()} Level={u.Level()} Height={u.Height()} Data={u.Data()?.Length ?? 0} new unit created with data");
            }
            else if (Conf.Pid == 0)
            {
                await Console.Out.WriteLineAsync($"PID={Conf.Pid} Epoch={u.EpochID()} Level={u.Level()} Height={u.Height()} new unit created");
            }

            try
            {
                await Send(u);
                await Update(u);
            }
            catch (Exception ex)
            {

            }
        }

        /// <summary>
        /// Returns a set of candidates such that their level is at most level - 1.
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public IUnit[] GetParentsForLevel(int level)
        {
            var result = new IUnit[Conf.NProc];
            IUnit u;
            foreach ((int i, var un) in Candidates.Index())
            {
                u = un;
                while (u != null && u.Level() >= level) u = u.Predecessor();
                result[i] = u;
            }

            MakeConsistent(result);
            return result;
        }

        /// <summary>
        /// Returns a copy of current parent candidates.
        /// </summary>
        /// <returns></returns>
        public IUnit[] GetParents()
        {
            var result = (IUnit[])Candidates.Clone();
            MakeConsistent(result);
            return result;
        }

        /// <summary>
        /// Tells the creator to stop updating parent candidates for the given pid and use the corresponding parent of our last created unit instead. Returns that parent.
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        public async Task<IUnit> FreezeParent(ushort pid)
        {
            await Mx.WaitAsync();
            
            try
            {
                var u = Candidates[Conf.Pid].Parents()[pid];
                Candidates[pid] = u;
                Frozen[pid] = true;
                Log.Warn().Val(Constants.Creator, pid).Msg(Constants.FreezedParent);

                return u;
            }
            finally
            {
                Mx.Release();
            }
        }

        /// <summary>
        /// Resets the candidates and all related variables to the initial state. Useful when switching to a new epoch.
        /// </summary>
        public void ResetEpoch()
        {
            for (int i = 0; i < Candidates.Length; i++)
            {
                Candidates[i] = null!;
            }

            MaxLevel = -1;
            OnMaxLevel = 0;
            Level = 0;
        }

        /// <summary>
        /// Puts the provided unit in parent candidates provided that the level is higher than the level of the previous candidate for that creator.
        /// </summary>
        /// <param name="u"></param>
        public void UpdateCandidates(IUnit u)
        {
            if (u.EpochID() != Epoch) return;

            var prev = Candidates[u.Creator()];
            if (prev == null || prev.Level() < u.Level())
            {
                Candidates[u.Creator()] = u;

                if (u.Level() == MaxLevel)
                {
                    OnMaxLevel++;
                }

                if (u.Level() > MaxLevel)
                {
                    MaxLevel = u.Level();
                    OnMaxLevel = 1;
                }

                Level = MaxLevel;
                if (OnMaxLevel >= Quorum)
                {
                    Level++;
                }
            }
        }

        /// <summary>
        /// Takes a unit that has been received from unit belt and updates the creator internal state with information contained in the unit.
        /// </summary>
        /// <param name="u"></param>
        public async Task Update(IUnit u)
        {
            Log.Debug().Val(Constants.Creator, u.Creator()).Val(Constants.Epoch, u.EpochID()).Val(Constants.Height, u.Height()).Val(Constants.Level, u.Level()).Val(Constants.Size, OnMaxLevel).Msg(Constants.CreatorProcessingUnit);

            // if the unit is from an older epoch or the units creator is known to be a forker, ignore it
            var success = Frozen.TryGetValue(u.Creator(), out var froz);
            if (success && (froz || u.EpochID() < Epoch)) return;

            /* if the unit is from a new epoch, switch to that epoch.
             * Since units appear on the belt in the order they were added to the DAG,
             * the first unit from a new epoch is always a dealing unit. */
            if (u.EpochID() > Epoch)
            {
                if (!EpochProof.Verify(u))
                {
                    Log.Warn().Val(Constants.Creator, u.Creator()).Val(Constants.Height, u.Height()).Msg(Constants.InvalidEpochProof);
                    return;
                }

                Log.Warn().Val(Constants.Level, Level).Val(Constants.Epoch, Epoch).Msg(Constants.SkippingEpoch);
                await NewEpoch(u.EpochID(), u.Data());
            }

            /* If this is a finishing unit, try to extract a threshold signature from it.
             * If there are enough shares to produce the signature (and thus a proof the epoch is finished), switch to a new epoch. */
            var epochProof = EpochProof.TryBuilding(u);
            if (epochProof != null)
            {
                if (u.EpochID() == Epoch)
                {
                    await NewEpoch(Epoch + 1, epochProof);
                }
                
                return;
            }

            UpdateCandidates(u);
        }

        /// <summary>
        /// Produces a piece of data to be included in a unit on a given level.
        /// For regular units, the provided IDataSource is used.
        /// For finishing units, it's either null, or, if available, an encoded threshold signature share of has hand ID of the last timing unit (obtained from PreblockMaker on lastTiming queue).
        /// </summary>
        /// <param name="level"></param>
        /// <param name="lastTiming"></param>
        /// <returns></returns>
        public async Task<byte[]> GetData(int level, Channel<IUnit> lastTiming)
        {
            if (level <= Conf.LastLevel)
            {
                if (Source != null) return await Source.Get();
            }

            // in a rare case there can be timing units from previous epochs left in the queue. The purpose of this loop is to drain and ignore them.
            while (true)
            {
                var success = lastTiming.Reader.TryRead(out var timingUnit);
                if (success)
                {
                    if (timingUnit!.EpochID() < Epoch) continue;

                    if (timingUnit.EpochID() == Epoch)
                    {
                        EpochDone = true;
                        if (Epoch == Conf.NumberOfEpochs - 1)
                        {
                            // the epoch we just finished is the last epoch we were supposed to produce
                            return Array.Empty<byte>();
                        }

                        return EpochProof.BuildShare(timingUnit);
                    }
                    Log.Warn().Val(Constants.Epoch, timingUnit.EpochID()).Msg(Constants.FutureLastTiming);
                }

                return Array.Empty<byte>();
            }
        }

        /// <summary>
        /// Checks if the creator is ready to produce a new unit. Usually that means: "do we have enough new candidates to produce a unit with level higher than the previous one?"
        /// Besides that, we stop producing units for the current epoch after creating a unit with a signature share.
        /// </summary>
        /// <returns></returns>
        public bool Ready()
        {
            return !EpochDone && Level > (Candidates[Conf.Pid]?.Level() ?? int.MinValue);
        }

        public (IUnit[] Parents, int Level) BuildParents()
        {
            if (Conf.CanSkipLevel)
            {
                return (GetParents(),  Level);
            }
            else
            {
                var level = Candidates[Conf.Pid].Level() + 1;
                return (GetParentsForLevel(level), level);
            }
        }

        /// <summary>
        /// Executes the main loop of the creator. Units appearing on the unit belt are examined and stored to be used as parents of future units.
        /// When there are enough new parents, a new unit is produced.
        /// LastTiming is a queue on which the last timing unit of each epoch is expected to appear.
        /// This method is stopped by closing the cancellation token.
        /// </summary>
        public async Task CreateUnits(Channel<IUnit> unitBelt, Channel<IUnit> lastTiming, IAlerter alerter, CancellationToken token)
        {
            var om = alerter.AddForkObserver(async (u, _) => await FreezeParent(u.Creator()));

            try
            {
                await NewEpoch(Epoch, Array.Empty<byte>());

                while (!token.IsCancellationRequested)
                {
                    IUnit u;
                    try
                    {
                        u = await unitBelt.Reader.ReadAsync(token);
                    }
                    catch (ChannelClosedException)
                    {
                        return;
                    }
                    if (token.IsCancellationRequested) return;

                    await Mx.WaitAsync(token);

                    // step 1: update candidates with all units waiting on belt
                    await Update(u);

                    while (Ready())
                    {
                        // 2. get parents and level using current strategy
                        (var parents, var level) = BuildParents();

                        // 3. make unit
                        Log.Debug().Val(Logging.Constants.Level, level).Val(Logging.Constants.Height, parents.Where(x => x != null).First().Height() + 1).Val("NumParents", parents.Length).Val(Logging.Constants.Creator, Conf.Pid).Msg("creator ready to produce unit");
                        await CreateUnit(parents, level, await GetData(level, lastTiming));
                    }

                    Mx.Release();
                }
            }
            finally
            {
                om.RemoveObserver();
                Log.Log().Msg(Constants.CreatorFinished);
            }
        }
    }
}
