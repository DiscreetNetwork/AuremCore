using Aurem.Config;
using Aurem.Model;
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
    public class Encoder : Stream
    {
        public Encoder(Stream s) { this.s = s; }

        public Stream Base => s;

        private Stream s;

        public override bool CanRead => s.CanRead;

        public override bool CanSeek => s.CanSeek;

        public override bool CanWrite => s.CanWrite;

        public override long Length => s.Length;

        public override long Position { get => s.Position; set => s.Position = value; }

        public override void Flush()
        {
            s.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return s.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return s.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            s.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            s.Write(buffer, offset, count);
        }

        /// <summary>
        /// Encodes the given crown and writes the data to the stream.
        /// </summary>
        /// <param name="crown"></param>
        public void EncodeCrown(Crown crown)
        {
            var nParents = (ushort)crown.Heights.Length;
            var data = new byte[34 + nParents*4];
            BinaryPrimitives.WriteUInt16LittleEndian(data, nParents);
            var s = 2;
            foreach (var h in crown.Heights)
            {
                if (h == -1) BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(s), uint.MaxValue);
                else BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(s), (uint)h);

                s += 4;
            }

            Array.Copy(crown.ControlHash.Data, 0, data, s, crown.ControlHash.Length);
            Write(data);
        }

        /// <summary>
        /// Encodes the given DagInfo and writes the data to the stream.
        /// </summary>
        /// <param name="dagInfo"></param>
        public void EncodeDagInfo(DagInfo dagInfo)
        {
            if (dagInfo is null)
            {
                var emptyData = new byte[4];
                BinaryPrimitives.WriteUInt32LittleEndian(emptyData, uint.MaxValue);
                Write(emptyData);
                return;
            }

            var nproc = (ushort)dagInfo.Heights.Length;
            var data = new byte[6 + nproc * 4];
            BinaryPrimitives.WriteUInt32LittleEndian(data, dagInfo.Epoch);
            BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(4), nproc);
            var s = 6;

            foreach (var h in dagInfo.Heights)
            {
                if (h == -1) BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(s), uint.MaxValue);
                else BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(s), (uint)h);

                s += 4;
            }

            Write(data);
        }

        public void EncodeUnit(IPreunit unit)
        {
            if (unit is null)
            {
                var empty = new byte[8];
                BinaryPrimitives.WriteUInt64LittleEndian(empty, ulong.MaxValue);
                Write(empty);
                return;
            }

            var data = new byte[72];
            BinaryPrimitives.WriteUInt64LittleEndian(data, unit.UnitID());
            Array.Copy(unit.Signature(), 0, data, 8, 64);
            Write(data);

            EncodeCrown(unit.View());

            var unitDataLen = (uint)(unit.Data()?.Length ?? 0);
            BinaryPrimitives.WriteUInt32LittleEndian(data, unitDataLen);
            Write(data.AsSpan(0, 4));
            if (unitDataLen > 0) Write(unit.Data());

            var rsDataLen = (uint)(unit.RandomSourceData()?.Length ?? 0);
            BinaryPrimitives.WriteUInt32LittleEndian(data, rsDataLen);
            Write(data.AsSpan(0, 4));
            if (rsDataLen > 0) Write(unit.RandomSourceData());
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

        public void EncodeUint32(uint i)
        {
            var bytes = new byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(bytes, i);
            Write(bytes);
        }
    }
}
