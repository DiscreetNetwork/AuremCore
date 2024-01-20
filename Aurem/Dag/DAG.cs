using Aurem.Common;
using Aurem.Model;
using Aurem.Model.Exceptions;
using Aurem.Persistence;
using Aurem.Units;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Dag
{
    /// <summary>
    /// Implements <see cref="IDag"/>.
    /// </summary>
    public class DAG : IDag
    {
        public ushort nProc;
        public uint Epoch;
        public int Session;
        public bool Setup;
        
        // structures to hold units in DAG.
        public UnitBag Units;
        public FiberMap LevelUnits;
        public FiberMap HeightUnits;
        public ISlottedUnits MaxUnits;

        // checks
        public List<UnitChecker> Checks;
        public List<InsertHook> PreInsert;
        public List<InsertHook> PostInsert;

        /// <summary>
        /// Constructs a DAG for a given number of processes.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="epochID"></param>
        public DAG(Config.Config conf, uint epochID)
        {
            nProc = conf.NProc;
            Session = conf.Session;
            Epoch = epochID;
            Units = new UnitBag();
            LevelUnits = new FiberMap(conf.NProc, conf.EpochLength);
            HeightUnits = new FiberMap(conf.NProc, conf.EpochLength);
            MaxUnits = new SlottedUnits(conf.NProc);
            Checks = new List<UnitChecker>(conf.Checks ?? Enumerable.Empty<UnitChecker>());
            PreInsert = new List<InsertHook>();
            PostInsert = new List<InsertHook>();
            Setup = conf.Setup;
        }

        public void AddCheck(UnitChecker check) => Checks.Add(check);

        public void BeforeInsert(InsertHook hook) => PreInsert.Add(hook);

        public void AfterInsert(InsertHook hook) => PostInsert.Add(hook);

        public uint EpochID() => Epoch;

        /// <summary>
        /// Checks if the given number of processes forms a quorum amongst all processes.
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public bool IsQuorum(ushort num) => num >= IDag.MinimalQuorum(nProc);

        /// <summary>
        /// Returns the number of processes which use the DAG.
        /// </summary>
        /// <returns></returns>
        public ushort NProc() => nProc;

        /// <summary>
        /// Returns the prime units at the requested level, indexed by their creator IDs.
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public ISlottedUnits UnitsOnLevel(int level)
        {
            (var res, var err) = LevelUnits.GetFiber(level);
            if (err is not null) return new SlottedUnits(nProc);

            return res;
        }

        /// <summary>
        /// Returns all units present in the dag that are above (in a height sense) the given heights.
        /// When called with null, returns all units in the DAG.
        /// Units returned by this method are not given in any specific order.
        /// </summary>
        /// <param name="heights"></param>
        /// <returns></returns>
        public List<IUnit> UnitsAbove(IList<int> heights)
        {
            if (heights is null) return Units.GetAll();

            return HeightUnits.Above(heights);
        }

        /// <summary>
        /// Returns the maximal units created by respective processes.
        /// </summary>
        /// <returns></returns>
        public ISlottedUnits MaximalUnitsPerProcess() => MaxUnits;

        /// <summary>
        /// Returns a unit with the given hash, if present in the DAG.
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public IUnit GetUnit(Hash hash) => Units.GetOne(hash);

        /// <summary>
        /// Returns an array of units corresponding to the hashes provided. If a unit of a give hash is not present in the DAG, the corresponding value is null.
        /// </summary>
        /// <param name="hashes"></param>
        /// <returns></returns>
        public IUnit[] GetUnits(IList<Hash> hashes) => Units.GetMany(hashes);

        /// <summary>
        /// Returns all units in the DAG with the given ID. Only in the case of forks will the returned list contain more than one element.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public List<IUnit> GetByID(ulong id)
        {
            (var height, var creator, var epoch) = IPreunit.DecodeID(id);
            if (epoch != EpochID()) return null;

            (var fiber, var err) = HeightUnits.GetFiber(height);
            if (err is not null) return null;

            return fiber.Get(creator);
        }

        public List<IUnit> DecodeParents(IPreunit pu)
        {
            var u = GetUnit(pu.Hash());
            if (u is not null) throw new DuplicateUnitException(u);

            var heights = pu.View().Heights;
            (var possibleParents, var unknown) = HeightUnits.Get(heights);
            if (unknown > 0) throw new UnknownParentsException(unknown);

            var parents = new IUnit[nProc];
            foreach ((var i, var units) in possibleParents.Index())
            {
                if (heights[i] == -1) continue;
                if (units.Count > 1) throw new AmbiguousParentsException(possibleParents.Select(x => x.ToArray()).ToArray());
                parents[i] = units[0];
            }

            return parents.ToList();
        }

        public IUnit BuildUnit(IPreunit pu, IList<IUnit> parents)
        {
            return new FreeUnit(pu, parents);
        }

        public async Task<Exception?> Check(IUnit u)
        {
            foreach (var check in Checks)
            {
                try
                {
                    await check(u, this);
                }
                catch (Exception ex)
                {
                    return ex;
                }
            }

            return null;
        }

        public void Insert(IUnit u)
        {
            u = UnitInDag.Embed(u, this);
            foreach (var hook in PreInsert)
            {
                hook(u);
            }

            UpdateUnitsOnHeight(u);
            UpdateUnitsOnLevel(u);
            Units.Add(u);
            UpdateMaximal(u);

            foreach (var hook in PostInsert)
            {
                hook(u);
            }
        }

        public void UpdateMaximal(IUnit u)
        {
            var creator = u.Creator();
            var maxByCreator = MaxUnits.Get(creator);
            var newMaxByCreator = new List<IUnit>();

            // works assuming no unit in the DAG created by creator is greater than or equal to u.
            foreach (var v in maxByCreator)
            {
                if (!IUnit.Above(u, v)) newMaxByCreator.Add(v);
            }

            newMaxByCreator.Add(u);
            MaxUnits.Set(creator, newMaxByCreator);
        }

        public void UpdateUnitsOnLevel(IUnit u)
        {
            if (u.Level() >= LevelUnits.Length())
            {
                LevelUnits.ExtendBy((u.Level() - LevelUnits.Length()) + 10); // 10 might not be perfectly optimal
            }

            (var su, _) = LevelUnits.GetFiber(u.Level());
            var creator = u.Creator();
            var oldPrimes = su.Get(creator);
            var primesByCreator = new List<IUnit>(oldPrimes);
            primesByCreator.Add(u);
            su.Set(creator, primesByCreator);
        }

        public void UpdateUnitsOnHeight(IUnit u)
        {
            var height = u.Height();
            var creator = u.Creator();

            if (height >= HeightUnits.Length())
            {
                HeightUnits.ExtendBy((u.Level() - HeightUnits.Length()) + 10); // 10 might not be perfectly optimal
            }

            (var su, _) = HeightUnits.GetFiber(height);
            var oldUnitsOnHeightByCreator = su.Get(creator);
            var unitsOnHeightByCreator = new List<IUnit>(oldUnitsOnHeightByCreator);
            unitsOnHeightByCreator.Add(u);
            su.Set(creator, unitsOnHeightByCreator);
        }

        public void Close()
        {
            DagDB.Instance.SaveDAG(this);
        }
    }
}
