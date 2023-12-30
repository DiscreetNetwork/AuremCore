using Aurem.Dag;
using Aurem.Model;
using Aurem.Serialize;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public byte[] Serialize()
        {
            using var ms = new MemoryStream();
            ms.Write(EncodeUtil.EncodeUnit(this));
            // encode extra data
            
            using var bw = new BinaryWriter(ms);
            bw.Write(_parents.Count);
            for (int i = 0; i < _parents.Count; i++)
            {
                if (_parents[i] == null)
                {
                    bw.Write(Model.Hash.ZeroHash.Data);
                }
                else
                {
                    bw.Write(_parents[i].Hash().Data);
                }
            }

            bw.Write(_level);

            // encode dict
            bw.Write(_floor?.Count ?? 0);

            if (_floor != null)
            {
                foreach ((var k, var v) in _floor)
                {
                    bw.Write(k);
                    bw.Write(v?.Count ?? 0);
                    for (int i = 0; i < (v?.Count ?? 0); i++)
                    {
                        if (v[i] == null)
                        {
                            bw.Write(Model.Hash.ZeroHash.Data);
                        }
                        else
                        {
                            bw.Write(v[i].Hash().Data);
                        }
                    }
                }
            }

            bw.Flush();
            return ms.ToArray();
        }

        public Func<DAG, IUnit> Deserialize(byte[] data)
        {
            using var ms = new MemoryStream(data);
            return Deserialize(ms);
        }

        public Func<DAG, IUnit> Deserialize(Stream s)
        {
            var dec = new Decoder(s);
            _preunit = dec.DecodePreunit();

            using var br = new BinaryReader(s);
            var plen = br.ReadInt32();
            List<Hash> parentHashes = new List<Hash>();
            for (int i = 0; i < plen; i++)
            {
                byte[] hashData = new byte[32];
                br.Read(hashData);
                parentHashes.Add(new Hash(hashData));
            }

            _level = br.ReadInt32();

            var fcount = br.ReadInt32();
            Dictionary<ushort, List<Hash>> floorTable = new Dictionary<ushort, List<Hash>>();

            if (fcount > 0)
            {
                for (int i = 0; i < fcount; i++)
                {
                    var k = br.ReadUInt16();
                    var l = br.ReadInt32();
                    var v = new List<Hash>();
                    for (int j = 0; j < l; j++)
                    {
                        byte[] hashData = new byte[32];
                        br.Read(hashData);
                        v.Add(new Hash(hashData));
                    }

                    floorTable[k] = v;
                }
            }

            return x => 
            {
                _parents = new List<IUnit>();

                for (int i = 0; i < plen; i++)
                {
                    if (parentHashes[i] == Model.Hash.ZeroHash)
                    {
                        _parents.Add(null!);
                    }
                    else
                    {
                        _parents.Add(x.GetUnit(parentHashes[i]));
                    }
                }
                
                _floor = new Dictionary<ushort, List<IUnit>>();

                foreach ((var k, var v) in floorTable)
                {
                    var l = new List<IUnit>();
                    foreach (var h in v)
                    {
                        if (h == Model.Hash.ZeroHash)
                        {
                            l.Add(null!);
                        }
                        else
                        {
                            l.Add(x.GetUnit(h));
                        }
                    }

                    _floor[k] = l;
                }

                return this;
            };
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

            if (_parents[pid] == null) return Enumerable.Empty<IUnit>();

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
                if (maximal.Count > 1 || (maximal.Count == 1 && !maximal[0].PEquals(_parents[pid])))
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
