﻿using Aurem.Config;
using Aurem.Model;
using Aurem.Random;
using AuremCore.Network;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Serialize
{
    /// <summary>
    /// Implements <see cref="Stream"/>. Thread-safe binary writer which encodes units in the following format:
    /// <list type="number">
    /// <item>Creator ID (2 bytes)</item>
    /// <item>Signature (64 bytes)</item>
    /// <item>Number of parents (2 bytes)</item>
    /// <item>Parent heights (4 bytes each)</item>
    /// <item>Control hash (32 bytes)</item>
    /// <item>Size of the unit data in bytes (4 bytes)</item>
    /// <item>Unit data</item>
    /// <item>Size of the random source data in bytes (4 bytes)</item>
    /// <item>Random source data</item>
    /// </list>
    /// All integers are represented as unsigned 32 or 16 bit values.
    /// </summary>
    public class Encoder
    {
        public Encoder(Stream s) { this.s = s; conn = null; }

        public Stream Base => s;

        private Stream? s;
        private Conn? conn;

        public Encoder(Conn conn)
        {
            this.conn = conn;
            s = null;
        }

        /// <summary>
        /// Encodes the given crown and writes the data to the stream.
        /// </summary>
        /// <param name="crown"></param>
        public void EncodeCrown(Crown crown)
        {
            if (conn != null)
            {
                conn.Write(SerializeCrown(crown)).Wait();
                return;
            }
            s!.Write(SerializeCrown(crown));
        }

        public static byte[] SerializeCrown(Crown crown)
        {
            var nParents = (ushort)crown.Heights.Length;
            var data = new byte[34 + nParents * 4];
            BinaryPrimitives.WriteUInt16LittleEndian(data, nParents);
            var s = 2;
            foreach (var h in crown.Heights)
            {
                if (h == -1) BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(s), uint.MaxValue);
                else BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(s), (uint)h);

                s += 4;
            }

            Array.Copy(crown.ControlHash.Data, 0, data, s, crown.ControlHash.Length);
            return data;
        }

        public async Task EncodeCrownAsync(Crown crown)
        {
            if (conn != null)
            {
                await conn.Write(SerializeCrown(crown));
                return;
            }
            await s!.WriteAsync(SerializeCrown(crown));
        }

        /// <summary>
        /// Encodes the given DagInfo and writes the data to the stream.
        /// </summary>
        /// <param name="dagInfo"></param>
        public void EncodeDagInfo(DagInfo dagInfo)
        {
            if (conn != null)
            {
                conn.Write(SerializeDagInfo(dagInfo)).Wait();
                return;
            }
            s!.Write(SerializeDagInfo(dagInfo));
        }

        public async Task EncodeDagInfoAsync(DagInfo dagInfo)
        {
            if (conn != null)
            {
                await conn.Write(SerializeDagInfo(dagInfo));
                return;
            }
            await s!.WriteAsync(SerializeDagInfo(dagInfo));
        }

        public static byte[] SerializeDagInfo(DagInfo dagInfo)
        {
            if (dagInfo is null)
            {
                var emptyData = new byte[4];
                BinaryPrimitives.WriteUInt32LittleEndian(emptyData, uint.MaxValue);
                return emptyData;
            }

            var nproc = (ushort)dagInfo.Heights.Length;
            var data = new byte[6 + nproc * 4];
            BinaryPrimitives.WriteUInt32LittleEndian(data, dagInfo.Epoch);
            BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(4), nproc);
            var s = 6;

            foreach (var h in dagInfo.Heights)
            {
                if (h == -1) BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(s), uint.MaxValue);
                else BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(s), (uint)h);

                s += 4;
            }

            return data;
        }

        public void EncodeUnit(IPreunit unit)
        {
            if (conn != null)
            {
                conn.Write(SerializeUnit(unit)).Wait();
                return;
            }
            s!.Write(SerializeUnit(unit));
        }

        public async Task EncodeUnitAsync(IPreunit unit)
        {
            if (conn != null)
            {
                await conn.Write(SerializeUnit(unit));
                return;
            }
            await s!.WriteAsync(SerializeUnit(unit));
        }

        public static byte[] SerializeUnit(IPreunit unit)
        {
            if (unit is null)
            {
                var empty = new byte[8];
                BinaryPrimitives.WriteUInt64LittleEndian(empty, ulong.MaxValue);
                return empty;
            }

            using var _ms = new MemoryStream();
            var data = new byte[72];
            BinaryPrimitives.WriteUInt64LittleEndian(data, unit.UnitID());
            Array.Copy(unit.Signature(), 0, data, 8, 64);
            _ms.Write(data);

            _ms.Write(SerializeCrown(unit.View()));

            var unitDataLen = (uint)(unit.Data()?.Length ?? 0);
            BinaryPrimitives.WriteUInt32LittleEndian(data, unitDataLen);
            _ms.Write(data.AsSpan(0, 4));
            if (unitDataLen > 0) _ms.Write(unit.Data());

            var rsDataLen = (uint)(unit.RandomSourceData()?.Length ?? 0);
            BinaryPrimitives.WriteUInt32LittleEndian(data, rsDataLen);
            _ms.Write(data.AsSpan(0, 4));
            if (rsDataLen > 0) _ms.Write(unit.RandomSourceData());

            return _ms.ToArray();
        }

        public void EncodeChunk(IEnumerable<IUnit> _units)
        {
            var units = _units.ToArray();
            if (units.Length >= Checks.MaxUnitsInChunk)
            {
                throw new Exception("chunk contains too many units");
            }

            EncodeUint32((uint)units.Length);

            foreach (var u in EncodeUtil.SortChunk(units))
            {
                EncodeUnit(u);
            }
        }

        public async Task EncodeChunkAsync(IEnumerable<IUnit> _units)
        {
            var units = _units.ToArray();
            if (units.Length >= Checks.MaxUnitsInChunk)
            {
                throw new Exception("chunk contains too many units");
            }

            var bytes = new byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(bytes, (uint)units.Length);
            if (conn != null)
            {
                await conn.Write(bytes);
            }
            else
            {
                await s!.WriteAsync(bytes);
            }

            foreach (var u in EncodeUtil.SortChunk(units))
            {
                await EncodeUnitAsync(u);
            }
        }

        public void EncodeUint32(uint i)
        {
            var bytes = new byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(bytes, i);
            if (conn != null)
            {
                conn.Write(bytes).Wait();
            }
            else
            {
                s!.Write(bytes);
            }
        }
    }
}
