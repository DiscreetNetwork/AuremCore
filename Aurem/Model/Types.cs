using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Model
{
    /// <summary>
    ///  UnitChecker is a function that performs a check on <see cref="IUnit"/> before Prepare.
    /// </summary>
    /// <param name="unit"></param>
    /// <param name="dag"></param>
    public delegate Task UnitChecker(IUnit unit, IDag dag);

    /// <summary>
    /// InsertHook is a function that performs some additional action on an <see cref="IUnit"/> before or after Insert (<see cref="IDag.BeforeInsert(InsertHook)"/>, <see cref="IDag.AfterInsert(InsertHook)"/>).
    /// </summary>
    /// <param name="unit"></param>
    public delegate void InsertHook(IUnit unit);

    /// <summary>
    /// PreblockMaker is a function that is called on a collection of units forming a timing round that was produced by <see cref="IOrderer"/>.
    /// </summary>
    /// <param name="units"></param>
    public delegate Task PreblockMaker(IList<IUnit> units);
}
