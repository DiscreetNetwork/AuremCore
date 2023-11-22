using Aurem.Config;
using Aurem.Model;
using Aurem.Ordering;
using Aurem.Units;
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
    /// Thread-safe binary reader which decodes units in the following format:
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
    public class Decoder
    {
        public Decoder(Stream s) { this.s = s; conn = null; }

        private Stream? s;
        private Conn? conn;

        public Decoder(Conn conn)
        {
            this.conn = conn;
            s = null;
        }

        private void Read(byte[] buf, int offset, int count)
        {
            if (conn != null)
            {
                conn.Read(buf[offset..count]).Wait();
            }
            else
            {
                s!.Read(buf, offset, count);
            }
        }

        /// <summary>
        /// WARNING: offset should almost always be zero.
        /// </summary>
        /// <param name="span"></param>
        /// <param name="buf"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static ReadOnlySpan<byte> Read(ReadOnlySpan<byte> span, byte[] buf, int offset, int count)
        {
            span.Slice(offset, count).CopyTo(buf);
            return span.Slice(offset + count);
        }

        public static ReadOnlySpan<byte> Read(ReadOnlySpan<byte> span, Span<byte> buf)
        {
            span[..buf.Length].CopyTo(buf);
            return span.Slice(buf.Length);
        }

        private Task<int> ReadAsync(byte[] buf, int offset, int count)
        {
            if (conn != null) return conn.Read(buf[offset..count]);

            return s!.ReadAsync(buf.AsMemory(offset, count)).AsTask();
        }

        private Task<int> ReadAsync(byte[] buf)
        {
            if (conn != null) return conn.Read(buf);

            return s!.ReadAsync(buf.AsMemory()).AsTask();
        }

        /// <summary>
        /// Reads encoded data from the stream and decodes it into a <see cref="Crown"/>.
        /// </summary>
        /// <returns></returns>
        public Crown DecodeCrown()
        {
            var shortBuf = new byte[2];
            var intBuf = new byte[4];

            Read(shortBuf, 0, 2);
            var nproc = BinaryPrimitives.ReadUInt16LittleEndian(shortBuf);

            var heights = new int[nproc];
            for (int i = 0; i < heights.Length; i++)
            {
                Read(intBuf, 0, 4);
                var h = BinaryPrimitives.ReadUInt32LittleEndian(intBuf);
                heights[i] = h == uint.MaxValue ? -1 : (int)h;
            }

            var controlHash = new Hash(new byte[32]);
            Read(controlHash.Data, 0, 32);

            return new Crown(heights, controlHash);
        }

        public static (Crown, int) DeserializeCrown(ReadOnlySpan<byte> data)
        {
            var nproc = BinaryPrimitives.ReadUInt16LittleEndian(data);
            data = data.Slice(2);

            var heights = new int[nproc];
            for (int i = 0; i < heights.Length; i++)
            {
                var h = BinaryPrimitives.ReadUInt32LittleEndian(data);
                data = data.Slice(4);
                heights[i] = h == uint.MaxValue ? -1 : (int)h;
            }

            var controlHash = new Hash(new byte[32]);
            Read(data, controlHash.Data);

            return (new Crown(heights, controlHash), 34 + nproc*4);
        }

        public async Task<Crown> DecodeCrownAsync()
        {
            var shortBuf = new byte[2];
            var intBuf = new byte[4];

            await ReadAsync(shortBuf, 0, 2);
            var nproc = BinaryPrimitives.ReadUInt16LittleEndian(shortBuf);

            var heightData = new byte[nproc * 4];
            var nhd = await ReadAsync(heightData);
            if (nhd != nproc * 4) throw new Exception("not enough data");

            var heights = new int[nproc];
            for (int i = 0; i < heights.Length; i++)
            {
                var h = BinaryPrimitives.ReadUInt32LittleEndian(heightData.AsSpan(4*i));
                heights[i] = h == uint.MaxValue ? -1 : (int)h;
            }

            var controlHash = new Hash(new byte[32]);
            await ReadAsync(controlHash.Data, 0, 32);

            return new Crown(heights, controlHash);
        }

        public DagInfo DecodeDagInfo()
        {
            var shortBuf = new byte[2];
            var intBuf = new byte[4];

            Read(intBuf, 0, 4);
            var epoch = BinaryPrimitives.ReadUInt32LittleEndian(intBuf);

            if (epoch == uint.MaxValue)
            {
                return null!;
            }

            Read(shortBuf, 0, 2);
            var nproc = BinaryPrimitives.ReadUInt16LittleEndian(shortBuf);

            var heights = new int[nproc];
            for (int i = 0; i < heights.Length; i++)
            {
                Read(intBuf, 0, 4);
                var h = BinaryPrimitives.ReadUInt32LittleEndian(intBuf);
                heights[i] = h == uint.MaxValue ? -1 : (int)h;
            }

            return new DagInfo { Epoch = epoch, Heights = heights };
        }

        public static (DagInfo, int) DeserializeDagInfo(ReadOnlySpan<byte> data)
        {
            var epoch = BinaryPrimitives.ReadUInt32LittleEndian(data);
            data = data.Slice(4);

            if (epoch == uint.MaxValue)
            {
                return (null!, 4);
            }

            var nproc = BinaryPrimitives.ReadUInt16LittleEndian(data);
            data = data.Slice(2);

            var heights = new int[nproc];
            for (int i = 0; i < heights.Length; i++)
            {
                var h = BinaryPrimitives.ReadUInt32LittleEndian(data);
                data = data.Slice(4);
                heights[i] = h == uint.MaxValue ? -1 : (int)h;
            }

            return (new DagInfo { Epoch = epoch, Heights = heights }, 6 + nproc*4);
        }

        public async Task<DagInfo> DecodeDagInfoAsync()
        {
            var shortBuf = new byte[2];
            var intBuf = new byte[4];

            await ReadAsync(intBuf, 0, 4);
            var epoch = BinaryPrimitives.ReadUInt32LittleEndian(intBuf);

            if (epoch == uint.MaxValue)
            {
                return null!;
            }

            await ReadAsync(intBuf, 0, 2);
            var nproc = BinaryPrimitives.ReadUInt16LittleEndian(shortBuf);

            var heightData = new byte[nproc * 4];
            var nhd = await ReadAsync(heightData);
            if (nhd != nproc * 4) throw new Exception("not enough data");

            var heights = new int[nproc];
            for (int i = 0; i < heights.Length; i++)
            {
                var h = BinaryPrimitives.ReadUInt32LittleEndian(intBuf);
                heights[i] = h == uint.MaxValue ? -1 : (int)h;
            }

            return new DagInfo { Epoch = epoch, Heights = heights };
        }

        public IPreunit DecodePreunit()
        {
            var longBuf = new byte[8];
            var intBuf = new byte[4];

            Read(longBuf, 0, 8);
            var id = BinaryPrimitives.ReadUInt64LittleEndian(longBuf);

            if (id == ulong.MaxValue)
            {
                return null!;
            }

            var sig = new byte[64];
            Read(sig, 0, 64);

            var crown = DecodeCrown();

            Read(intBuf, 0, 4);
            var unitDataLen = BinaryPrimitives.ReadUInt32LittleEndian(intBuf);
            if (unitDataLen > Checks.MaxDataBytesPerUnit) throw new Exception("maximum allowed data size in a preunit exceeded");
            var unitData = new byte[unitDataLen];
            Read(unitData, 0, (int)unitDataLen);

            Read(intBuf, 0, 4);
            var rsDataLen = BinaryPrimitives.ReadUInt32LittleEndian(intBuf);
            if (rsDataLen > Checks.MaxRandomSourceDataBytesPerUnit) throw new Exception("maximum allowed random source data size in a preunit exceeded");
            var rsData = new byte[rsDataLen];
            Read(rsData, 0, (int)rsDataLen);

            return new Preunit(id, crown, unitData, rsData, sig);
        }

        public static (IPreunit, int) DeserializePreunit(ReadOnlySpan<byte> data)
        {
            var id = BinaryPrimitives.ReadUInt64LittleEndian(data);
            data = data.Slice(8);

            if (id == ulong.MaxValue)
            {
                return (null!, 8);
            }

            var sig = new byte[64];
            data = Read(data, sig);

            (var crown, var crownLen) = DeserializeCrown(data);
            data = data.Slice(crownLen);

            var unitDataLen = BinaryPrimitives.ReadUInt32LittleEndian(data);
            data = data.Slice(4);
            if (unitDataLen > Checks.MaxDataBytesPerUnit) throw new Exception("maximum allowed data size in a preunit exceeded");
            var unitData = new byte[unitDataLen];
            data = Read(data, unitData);

            var rsDataLen = BinaryPrimitives.ReadUInt32LittleEndian(data);
            data = data.Slice(4);
            if (rsDataLen > Checks.MaxRandomSourceDataBytesPerUnit) throw new Exception("maximum allowed random source data size in a preunit exceeded");
            var rsData = new byte[rsDataLen];
            data = Read(data, rsData);

            return (new Preunit(id, crown, unitData, rsData, sig), 80 + crownLen + (int)(unitDataLen + rsDataLen));
        }

        public async Task<IPreunit> DecodePreunitAsync()
        {
            var longBuf = new byte[8];
            var intBuf = new byte[4];

            await ReadAsync(longBuf, 0, 8);
            var id = BinaryPrimitives.ReadUInt64LittleEndian(longBuf);

            if (id == ulong.MaxValue)
            {
                return null!;
            }

            var sig = new byte[64];
            await ReadAsync(sig, 0, 64);

            var crown = await DecodeCrownAsync();

            await ReadAsync(intBuf, 0, 4);
            var unitDataLen = BinaryPrimitives.ReadUInt32LittleEndian(intBuf);
            if (unitDataLen > Checks.MaxDataBytesPerUnit) throw new Exception("maximum allowed data size in a preunit exceeded");
            var unitData = new byte[unitDataLen];
            await ReadAsync(unitData, 0, (int)unitDataLen);

            await ReadAsync(intBuf, 0, 4);
            var rsDataLen = BinaryPrimitives.ReadUInt32LittleEndian(intBuf);
            if (rsDataLen > Checks.MaxRandomSourceDataBytesPerUnit) throw new Exception("maximum allowed random source data size in a preunit exceeded");
            var rsData = new byte[rsDataLen];
            await ReadAsync(rsData, 0, (int)rsDataLen);

            return new Preunit(id, crown, unitData, rsData, sig);
        }

        public IPreunit[] DecodeChunk()
        {
            var k = DecodeUint32();

            if (k > Checks.MaxUnitsInChunk) throw new Exception("chunk contains too many units");

            var result = new IPreunit[k];
            for (int i = 0; i < k; i++)
            {
                result[i] = DecodePreunit();
            }

            return result;
        }

        public static (IPreunit[], int) DeserializeChunk(ReadOnlySpan<byte> data)
        {
            var k = BinaryPrimitives.ReadUInt32LittleEndian(data);
            data = data.Slice(4);

            if (k > Checks.MaxUnitsInChunk) throw new Exception("chunk contains too many units");

            var result = new IPreunit[k];
            var tot = 4;
            for (int i = 0; i < k; i++)
            {
                (result[i], var t) = DeserializePreunit(data);
                tot += t;
            }

            return (result, tot);
        }

        public async Task<IPreunit[]> DecodeChunkAsync()
        {
            var buf = new byte[4];
            await ReadAsync(buf, 0, 4);
            var k = BinaryPrimitives.ReadUInt32LittleEndian(buf);

            if (k > Checks.MaxUnitsInChunk) throw new Exception("chunk contains too many units");

            var result = new IPreunit[k];
            for (int i = 0; i < k; i++)
            {
                result[i] = await DecodePreunitAsync();
            }

            return result;
        }

        public uint DecodeUint32()
        {
            var buf = new byte[4];
            Read(buf, 0, 4);
            return BinaryPrimitives.ReadUInt32LittleEndian(buf);
        }
    }
}
