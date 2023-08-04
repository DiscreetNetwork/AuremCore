using Aurem.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Units
{
    /// <summary>
    /// Represents an <see cref="IUnit"/> which has yet to be added to the DAG.
    /// </summary>
    public class FreeUnit : IPreunit, IUnit
    {
        private IPreunit _preunit;

        private List<IUnit> _parents;
        private int _level;
        private Dictionary<ushort, List<IUnit>> _floor;

        public FreeUnit() { }

        public FreeUnit(ushort creator, uint epoch, IList<IUnit> parents, int level, byte[] data, byte[] rsData, IPrivateKey pk)
        {
            var crown = new Crown(parents);
            var height = crown.Heights[creator] + 1;
            var id = IPreunit.ID(height, creator, epoch);
            var hash = Units.Preunit.ComputeHash(id, crown, data, rsData);
            var signature = pk.Sign(hash);

            _preunit = new Preunit(id, crown, data, rsData, signature);
            _parents = new List<IUnit>(parents);
            _level = level;

            ComputeFloor();
        }

        public FreeUnit(IPreunit pu, IList<IUnit> parents)
        {
            _preunit = pu;
            _parents = new List<IUnit>(parents);
            _level = IUnit.LevelFromParents(parents);

            ComputeFloor();
        }

        public IList<IUnit> Parents() => _parents;

        public int Level() => _level;

        public IEnumerable<IUnit> Floor(ushort pid)
        {
            if (_floor.ContainsKey(pid))
            {
                return _floor[pid];
            }

            if (_parents[pid] == null) return null;

            return _parents.GetRange(pid, 1);
        }

        public bool AboveWithinProc(IUnit v)
        {
            if (Creator() != v.Creator()) return false;
            IUnit w;
            for (w = this; w != null && w.Height() > v.Height(); w = w.Predecessor())
            {

            }

            if (w == null) return false;

            return w.Hash().Equals(v.Hash());
        }

        public void ComputeFloor()
        {
            _floor = new();
            if (_preunit.Dealing()) return;

            for (ushort pid = 0; pid < _parents.Count; pid++)
            {
                var maximal = IUnit.MaximalByPid(_parents, pid).ToList();
                if (maximal.Count > 1 || (maximal.Count == 1 && !maximal[0].Equals(_parents[pid])))
                {
                    _floor[pid] = maximal;
                }
            }
        }

        public uint EpochID()
        {
            return _preunit.EpochID();
        }

        public ushort Creator()
        {
            return _preunit.Creator();
        }

        public byte[] Signature()
        {
            return _preunit.Signature();
        }

        public Hash Hash()
        {
            return _preunit.Hash();
        }

        public int Height()
        {
            return _preunit.Height();
        }

        public Crown View()
        {
            return _preunit.View();
        }

        public byte[] Data()
        {
            return _preunit.Data();
        }

        public byte[] RandomSourceData()
        {
            return _preunit.RandomSourceData();
        }
    }
}
