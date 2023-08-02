using Aurem.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Units
{
    /// <summary>
    /// A unit that is already inside the DAG and has all its properties precomputed and cached. It uses forking heights to optimize AboveWithinProc calls.
    /// </summary>
    public class UnitInDag : IUnit
    {
        private IUnit _unit;
        private int _forkingHeight;

        /// <summary>
        /// Transforms the given unit into a UnitInDag and computes the forking height. The retuned unit overrides <see cref="IUnit.AboveWithinProc(IUnit)"/> to use the forking height.
        /// </summary>
        /// <param name="u"></param>
        /// <param name="dag"></param>
        /// <returns></returns>
        public static IUnit Embed(IUnit u, IDag dag)
        {
            var result = new UnitInDag { _unit = u, _forkingHeight = int.MaxValue };
            result.ComputeForkingHeight(dag);

            return result;
        }

        public static int CommonForkingHeight(UnitInDag u, UnitInDag v)
        {
            if (u._forkingHeight < v._forkingHeight)
            {
                return u._forkingHeight;
            }

            return v._forkingHeight;
        }

        public bool AboveWithinProc(IUnit v)
        {
            if (Height() < v.Height() || Creator() != v.Creator())
            {
                return false;
            }

            if (v is UnitInDag vInDag)
            {
                if (v.Height() <= CommonForkingHeight(this, vInDag))
                {
                    return true;
                }
            }

            // either (1) fork or (2) different unit type.
            return _unit.AboveWithinProc(v);
        }

        public void ComputeForkingHeight(IDag dag)
        {
            // this implementation works only if units created by a single process are added atomically to dag.MaxUnits (i.e. no race condition for writing/reading).
            if (_unit.Dealing())
            {
                if (dag.MaximalUnitsPerProcess().Get(Creator()).Count > 0)
                {
                    _forkingHeight = -1;
                }
                else
                {
                    _forkingHeight = int.MaxValue;
                }
                return;
            }

            if (((IUnit)this).Predecessor() is UnitInDag predecessor)
            {
                var found = false;
                foreach (var v in dag.MaximalUnitsPerProcess().Get(Creator()))
                {
                    if (v == predecessor)
                    {
                        found = true; break;
                    }
                }
                if (found)
                {
                    _forkingHeight = predecessor._forkingHeight;
                }
                else
                {
                    // there is already a unit that has predecessor (the variable) as a predecessor, so this unit is a fork
                    if (predecessor._forkingHeight < predecessor.Height())
                    {
                        _forkingHeight = predecessor._forkingHeight;
                    }
                    else
                    {
                        _forkingHeight = predecessor.Height();
                    }
                }
            }
        }

        public ushort Creator()
        {
            return _unit.Creator();
        }

        public byte[] Data()
        {
            return _unit.Data();
        }

        public uint EpochID()
        {
            return _unit.EpochID();
        }

        public IEnumerable<IUnit> Floor(ushort id)
        {
            return _unit.Floor(id);
        }

        public Hash Hash()
        {
            return _unit.Hash();
        }

        public int Height()
        {
            return _unit.Height();
        }

        public int Level()
        {
            return _unit.Level();
        }

        public IEnumerable<IUnit> Parents()
        {
            return _unit.Parents();
        }

        public byte[] RandomSourceData()
        {
            return _unit.RandomSourceData();
        }

        public byte[] Signature()
        {
            return _unit.Signature();
        }

        public Crown View()
        {
            return _unit.View();
        }
    }
}
