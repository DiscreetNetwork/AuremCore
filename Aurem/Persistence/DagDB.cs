﻿using Aurem.Dag;
using Aurem.Model;
using Aurem.Serialize;
using Aurem.Units;
using RocksDbSharp;
using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Persistence
{
    public class DagDB
    {
        private static DagDB _instance;

        public static DagDB Instance { get { return _instance; } }

        static DagDB()
        {
            var home = (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX) ? Environment.GetEnvironmentVariable("HOME") : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");

            var dagpath = Path.Combine($"{home}", "aurem");
            _instance = new DagDB(dagpath);
        }

        // unit keys: sessionID|setup|epochID|mapID|level|creator|index (4 + 1 + 4 + 1 + 2 + 2 + 4 = 17 bytes)
        public ColumnFamilyHandle Units;
        public ColumnFamilyHandle Commitments;

        // keys: Hash (32 bytes) ; values are the unit keys
        public ColumnFamilyHandle UnitsByHash;

        public const string UNITS = "units";
        public const string UNITS_BY_HASH = "units_by_hash";
        public const string COMMITMENTS = "commitments";

        public const int MaxUnitsID = 1;
        public const int LevelUnitsID = 2;
        public const int HeightUnitsID = 3;

        public static byte[] ZEROKEY = new byte[13];

        private string folder;
        private int sessionID;

        public string Folder => folder;
        public int SessionID => sessionID;

        private RocksDb rdb;
        
        public DagDB(string path)
        {
            try
            {
                if (File.Exists(path)) throw new Exception("DagDB expects a valid directory path, not a file");

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                folder = Path.Combine(path, $"persistence");

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                var options = new DbOptions().SetCreateIfMissing().SetCreateMissingColumnFamilies().SetKeepLogFileNum(5).SetMaxTotalWalSize(100UL * 1048576UL);

                var _colFamilies = new ColumnFamilies
                {
                    new ColumnFamilies.Descriptor(UNITS, new ColumnFamilyOptions().SetCompression(Compression.Lz4)),
                    new ColumnFamilies.Descriptor(UNITS_BY_HASH, new ColumnFamilyOptions().SetCompression(Compression.Lz4)),
                    new ColumnFamilies.Descriptor(COMMITMENTS, new ColumnFamilyOptions().SetCompression(Compression.Lz4)),
                };

                rdb = RocksDb.Open(options, folder, _colFamilies);

                Units = rdb.GetColumnFamily(UNITS);
                UnitsByHash = rdb.GetColumnFamily(UNITS_BY_HASH);
                Commitments = rdb.GetColumnFamily(COMMITMENTS);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to create the DagDB");
                throw new Exception($"DagDB failed to create or open the database: {ex.Message}");
            }
        }

        private static byte[] DataToIndex(int sessionID, byte setup, uint epochID, int mapID, int level, int creator, int index)
        {
            var rv = new byte[18];
            BinaryPrimitives.WriteUInt32LittleEndian(rv, (uint)sessionID);
            rv[4] = setup;
            BinaryPrimitives.WriteUInt32LittleEndian(rv.AsSpan(5), epochID);
            rv[9] = (byte)mapID;
            BinaryPrimitives.WriteUInt16LittleEndian(rv.AsSpan(10), (ushort)level);
            BinaryPrimitives.WriteUInt16LittleEndian(rv.AsSpan(12), (ushort)creator);
            BinaryPrimitives.WriteUInt32LittleEndian(rv.AsSpan(14), (uint)index);

            return rv;
        }

        private static byte[] DataToUnitIDIndex(int sessionID, byte setup, ulong unitID)
        {
            byte[] rv = new byte[13];
            BinaryPrimitives.WriteUInt32LittleEndian(rv, (uint)sessionID);
            rv[4] = setup;
            BinaryPrimitives.WriteUInt64LittleEndian(rv.AsSpan(5), unitID);
            return rv;
        }

        private static (int SessionID, byte Setup, uint EpochID, byte MapID, ushort Level, ushort Creator, int Index) IndexToData(byte[] data)
        {
            var sessionID = BinaryPrimitives.ReadUInt32LittleEndian(data);
            var setup = data[4];
            var epochID = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(5));
            var mapID = data[9];
            var level = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(10));
            var creator = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(12));
            var index = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(14));

            return ((int)sessionID, setup, epochID, mapID, level, creator, (int)index);
        }

        public DAG ReconstructDag(DAG dag)
        {
            byte setup = (byte)(dag.Setup ? 1 : 0);
            var lower = DataToIndex(sessionID, setup, dag.EpochID(), MaxUnitsID, 0, 0, 0);
            var upper = DataToIndex(sessionID, setup, dag.EpochID(), HeightUnitsID, ushort.MaxValue, ushort.MaxValue, int.MaxValue);
            var iter = rdb.NewIterator(cf: Units, new ReadOptions().SetIterateLowerBound(lower).SetIterateUpperBound(upper));
            iter.SeekToFirst();

            Dictionary<ushort, Dictionary<ushort, List<IUnit>>> levelUnits = new Dictionary<ushort, Dictionary<ushort, List<IUnit>>>();
            Dictionary<ushort, Dictionary<ushort, List<IUnit>>> heightUnits = new Dictionary<ushort, Dictionary<ushort, List<IUnit>>>();
            Dictionary<ushort, List<IUnit>> maxUnits = new Dictionary<ushort, List<IUnit>>();
            List<Func<DAG, IUnit>> finalizeConstruction = new List<Func<DAG, IUnit>>();

            while (iter.Valid())
            {
                var idx = IndexToData(iter.Key());
                if (idx.SessionID != sessionID || idx.EpochID != dag.EpochID() || idx.Setup != setup) break;

                Dictionary<ushort, List<IUnit>> d;

                if (idx.MapID == MaxUnitsID)
                {
                    d = maxUnits;
                }
                else if (idx.MapID == LevelUnitsID)
                {
                    if (!levelUnits.ContainsKey(idx.Level))
                    {
                        levelUnits[idx.Level] = new Dictionary<ushort, List<IUnit>>();
                    }

                    d = levelUnits[idx.Level];
                }
                else if (idx.MapID == HeightUnitsID)
                {
                    if (!heightUnits.ContainsKey(idx.Level))
                    {
                        heightUnits[idx.Level] = new Dictionary<ushort, List<IUnit>>();
                    }

                    d = heightUnits[idx.Level];
                }
                else
                {
                    throw new Exception($"unexpected MapID: {idx.MapID}");
                }

                if (!d.ContainsKey(idx.Creator))
                {
                    d[idx.Creator] = new List<IUnit>();
                }

                var l = d[idx.Creator];
                if (l != null && idx.Index >= l.Count)
                {
                    while (idx.Index != l.Count)
                    {
                        l.Add(null!);
                    }

                    (var u, var f) = new UnitInDag().Deserialize(iter.Value());
                    l.Add(u);
                    finalizeConstruction.Add(f);

                    if (dag.Units.GetOne(u.Hash()) == null)
                    {
                        dag.Units.Add(u);
                    }
                }
                else if (l != null)
                {
                    (var u, var f) = new UnitInDag().Deserialize(iter.Value());
                    l[idx.Index] = u;
                    finalizeConstruction.Add(f);

                    if (dag.Units.GetOne(u.Hash()) == null)
                    {
                        dag.Units.Add(u);
                    }
                }

                iter = iter.Next();
            }

            iter.Dispose();

            // unit bag has been reconstructed, so we can finalize the structure of the units
            foreach (var f in finalizeConstruction)
            {
                f(dag);
            }

            // now reconstruct the maxUnits
            foreach ((var k, var v) in maxUnits)
            {
                dag.MaxUnits.Set(k, v);
            }

            // now level units
            foreach ((var lvl, var slot) in levelUnits)
            {
                if (lvl >= dag.LevelUnits.Length())
                {
                    dag.LevelUnits.ExtendBy((lvl - dag.LevelUnits.Length()) + 10); // 10 might not be perfectly optimal
                }

                (var su, _) = dag.LevelUnits.GetFiber(lvl);
                if (su != null)
                {
                    foreach ((var k, var v) in slot)
                    {
                        su.Set(k, v);
                    }
                }
            }

            // now heights... (same as level)
            foreach ((var h, var slot) in heightUnits)
            {
                if (h >= dag.HeightUnits.Length())
                {
                    dag.HeightUnits.ExtendBy((h - dag.HeightUnits.Length()) + 10); // 10 might not be perfectly optimal
                }

                (var su, _) = dag.HeightUnits.GetFiber(h);
                if (su != null)
                {
                    foreach ((var k, var v) in slot)
                    {
                        su.Set(k, v);
                    }
                }
            }

            // dag is reconstructed
            return dag;
        }

        // often times we just need to get the preunit associated with a hash instead...
        public IPreunit GetUnit(Hash hash)
        {
            var res = rdb.Get(hash.Data, cf: UnitsByHash);

            if (res == null)
            {
                return null!;
            }

            return GetUnit(res);
        }

        public IPreunit GetUnit(byte[] key)
        {
            var res = rdb.Get(key, cf: Units);

            if (res == null)
            {
                return null!;
            }

            // the encoded unit structure ALWAYS stores the completed preunit first. Extra data is appended.
            return EncodeUtil.DecodeUnit(res);
        }

        public List<IPreunit> GetByID(int session, ulong id)
        {
            (var height, var creator, var epoch) = IPreunit.DecodeID(id);

            List<IPreunit> units = new List<IPreunit>();
            IPreunit res;
            var i = 0;
            do
            {
                // TODO: setup vs consensus unit checks
                var idx = DataToIndex(session, 0, epoch, HeightUnitsID, height, creator, i);
                res = GetUnit(idx);
                if (res == null) break;
                units.Add(res);
                i += 1;
            }
            while (res != null);

            return units;
        }

        public void SaveDAG(DAG dag)
        {
            WriteBatch batch = new WriteBatch();
            Dictionary<Hash, byte[]> hashesToIndices = new Dictionary<Hash, byte[]>(new Hash.HashEqualityComparer());
            byte setup = (byte)(dag.Setup ? 1 : 0);


            bool SUIterate(int k, List<IUnit> v, byte mapID, int lvl)
            {
                if (v == null) return true;
                for (int i = 0; i < v.Count; i++)
                {
                    if (v[i] is not UnitInDag) continue;
                    var u = (UnitInDag)v[i];

                    var idx = DataToIndex(sessionID, setup, dag.EpochID(), mapID, lvl, k, i);
                    batch.Put(idx, u.Serialize(), cf: Units);
                    hashesToIndices[u.Hash()] = idx;
                }

                return true;
            }

            dag.MaxUnits.Iterate((k, v) =>
            {
                return SUIterate(k, v, MaxUnitsID, 0);
            });

            dag.HeightUnits.Iterate((lvl, su) =>
            {
                su.Iterate((k, v) =>
                {
                    return SUIterate(k, v, HeightUnitsID, lvl);
                });

                return true;
            });

            dag.LevelUnits.Iterate((lvl, su) =>
            {
                su.Iterate((k, v) =>
                {
                    return SUIterate(k, v, LevelUnitsID, lvl);
                });

                return true;
            });

            foreach ((var k, var v) in hashesToIndices)
            {
                batch.Put(k.Data, v, cf: UnitsByHash);
            }

            rdb.Write(batch);
            Console.WriteLine("Saved the DAG.");
        }
    }
}
