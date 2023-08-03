using AuremCore.Crypto.Threshold;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Model
{
    /// <summary>
    /// Represents the main data structure of the top-level consensus protocol. It is built of units partially ordered by "is-parent-of" relation.
    /// </summary>
    public interface IDag
    {
        /// <summary>
        /// Returns an epoch ID, a unique identifier of the epoch for this DAG instance.
        /// </summary>
        /// <returns></returns>
        public uint EpochID();

        /// <summary>
        /// Returns a <see cref="List{T}"/> of <see cref="IUnit"/> parents of the given preunit, if the control hash matches.
        /// </summary>
        /// <param name="preunit">The preunit to get the parents of.</param>
        /// <returns></returns>
        public List<IUnit> DecodeParents(IPreunit preunit);

        /// <summary>
        /// Constructs a new unit from the preunit and the slice of parents.
        /// </summary>
        /// <param name="preunit"></param>
        /// <param name="parents"></param>
        /// <returns></returns>
        public IUnit BuildUnit(IPreunit preunit, IList<IUnit> parents);

        /// <summary>
        /// Runs, on the given unit, a series of UnitCheckers added to the DAG with AddCheck.
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        public Exception Check(IUnit unit);

        /// <summary>
        /// Puts a unit into the DAG.
        /// </summary>
        /// <param name="unit"></param>
        public void Insert(IUnit unit);

        /// <summary>
        /// Returns all units on a given level of the DAG.
        /// </summary>
        /// <param name="level"></param>
        /// <returns>An <see cref="ISlottedUnits"/> of the units on the specified level.</returns>
        public ISlottedUnits UnitsOnLevel(int level);

        /// <summary>
        /// Returns a collection of units containing, for each process, all maximal units created by that process.
        /// </summary>
        /// <returns>An <see cref="ISlottedUnits"/> of the maximal units for each process.</returns>
        public ISlottedUnits MaximalUnitsPerProcess();

        /// <summary>
        /// Returns all units present in the DAG with heights heigher than the given array of heights (of length nproc).
        /// </summary>
        /// <param name="heights"></param>
        /// <returns>A <see cref="List{T}"/> of <see cref="IUnit"/>.</returns>
        public List<IUnit> UnitsAbove(int[] heights);

        /// <summary>
        /// Returns a unit with the given hash, if present in the DAG.
        /// </summary>
        /// <param name="hash"></param>
        /// <returns>An <see cref="IUnit"/> corresponding to the hash, or <see langword="null"/> otherwise.</returns>
        public IUnit GetUnit(Hash hash);

        /// <summary>
        /// Returns an array of units with the given hashes, in the same order as requested. If no unit with the particular hash exists in the DAG, the result contains <see langword="null"/> in the corresponding position.
        /// </summary>
        /// <param name="hash"></param>
        /// <returns>An array of <see cref="IUnit"/>.</returns>
        public IUnit[] GetUnits(Hash[] hashes);

        /// <summary>
        /// Attempts to return the singular unit associated with the given ID. In the case of forks, the returned array will contain more than one element.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IUnit[] GetByID(ulong id);

        /// <summary>
        /// Checks if the given number of processes is enough to form a quorum.
        /// </summary>
        /// <param name="nproc">The number of processes to check.</param>
        /// <returns></returns>
        public bool IsQuorum(ushort nproc);

        /// <summary>
        /// Returns the number of processes that shares this DAG.
        /// </summary>
        /// <returns></returns>
        public ushort NProc();

        /// <summary>
        /// Extends the list of <see cref="UnitChecker"/> that are used by this DAG during the addition of a unit.
        /// </summary>
        /// <param name="checker">The <see cref="UnitChecker"/> to add.</param>
        public void AddCheck(UnitChecker checker);

        /// <summary>
        /// Adds an action to perform before insertion, represented by the supplied <see cref="InsertHook"/>.
        /// </summary>
        /// <param name="hook"></param>
        public void BeforeInsert(InsertHook hook);

        /// <summary>
        /// Adds an action to perform after insertion, represented by the supplied <see cref="InsertHook"/>.
        /// </summary>
        /// <param name="hook"></param>
        public void AfterInsert(InsertHook hook);

        /// <summary>
        /// Returns the minimal possible size of a subset forminga quorum within nProcesses.
        /// </summary>
        /// <param name="nProcesses"></param>
        public static ushort MinimalQuorum(ushort nProcesses) => TUtil.MinimalQuorum(nProcesses);

        /// <summary>
        /// Returns the minimal size of a subset of nProcesses that guarantees the subset contains at least one honest process.
        /// </summary>
        /// <param name="nProcesses"></param>
        /// <returns></returns>
        public static ushort MinimalTrusted(ushort nProcesses) => TUtil.MinimalTrusted(nProcesses); 
    }
}
