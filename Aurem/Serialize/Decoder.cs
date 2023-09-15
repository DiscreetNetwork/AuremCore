using Aurem.Config;
using Aurem.Model;
using Aurem.Units;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Serialize
{
    /// <summary>
    /// Implements <see cref="Stream"/>. Thread-safe binary reader which decodes units in the following format:
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
    public class Decoder : Stream
    {
        public Decoder(Stream s) { this.s = s; }

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
                heights[h] = h == uint.MaxValue ? -1 : (int)h;
            }

            var controlHash = new Hash(new byte[32]);
            Read(controlHash.Data, 0, 32);

            return new Crown(heights, controlHash);
        }

        public async Task<Crown> DecodeCrownAsync()
        {
            var shortBuf = new byte[2];
            var intBuf = new byte[4];

            await s.ReadAsync(shortBuf.AsMemory(0, 2));
            var nproc = BinaryPrimitives.ReadUInt16LittleEndian(shortBuf);

            var heightData = new byte[nproc * 4];
            var nhd = await s.ReadAsync(heightData);
            if (nhd != nproc * 4) throw new Exception("not enough data");

            var heights = new int[nproc];
            for (int i = 0; i < heights.Length; i++)
            {
                var h = BinaryPrimitives.ReadUInt32LittleEndian(heightData.AsSpan(4*i));
                heights[h] = h == uint.MaxValue ? -1 : (int)h;
            }

            var controlHash = new Hash(new byte[32]);
            await s.ReadAsync(controlHash.Data.AsMemory(0, 32));

            return new Crown(heights, controlHash);
        }

        public DagInfo DecodeDagInfo()
        {
            var shortBuf = new byte[2];
            var intBuf = new byte[4];

            Read(intBuf, 0, 4);
            var epoch = BinaryPrimitives.ReadUInt32LittleEndian(intBuf);

            Read(shortBuf, 0, 2);
            var nproc = BinaryPrimitives.ReadUInt16LittleEndian(shortBuf);

            var heights = new int[nproc];
            for (int i = 0; i < heights.Length; i++)
            {
                Read(intBuf, 0, 4);
                var h = BinaryPrimitives.ReadUInt32LittleEndian(intBuf);
                heights[h] = h == uint.MaxValue ? -1 : (int)h;
            }

            return new DagInfo { Epoch = epoch, Heights = heights };
        }

        public async Task<DagInfo> DecodeDagInfoAsync()
        {
            var shortBuf = new byte[2];
            var intBuf = new byte[4];

            await s.ReadAsync(intBuf.AsMemory(0, 4));
            var epoch = BinaryPrimitives.ReadUInt32LittleEndian(intBuf);

            await s.ReadAsync(intBuf.AsMemory(0, 2));
            var nproc = BinaryPrimitives.ReadUInt16LittleEndian(shortBuf);

            var heightData = new byte[nproc * 4];
            var nhd = await s.ReadAsync(heightData);
            if (nhd != nproc * 4) throw new Exception("not enough data");

            var heights = new int[nproc];
            for (int i = 0; i < heights.Length; i++)
            {
                var h = BinaryPrimitives.ReadUInt32LittleEndian(intBuf);
                heights[h] = h == uint.MaxValue ? -1 : (int)h;
            }

            return new DagInfo { Epoch = epoch, Heights = heights };
        }

        public IPreunit DecodePreunit()
        {
            var longBuf = new byte[8];
            var intBuf = new byte[4];

            Read(longBuf, 0, 8);
            var id = BinaryPrimitives.ReadUInt64LittleEndian(longBuf);

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

        public async Task<IPreunit> DecodePreunitAsync()
        {
            var longBuf = new byte[8];
            var intBuf = new byte[4];

            await s.ReadAsync(longBuf.AsMemory(0, 8));
            var id = BinaryPrimitives.ReadUInt64LittleEndian(longBuf);

            var sig = new byte[64];
            await s.ReadAsync(sig.AsMemory(0, 64));

            var crown = DecodeCrown();

            await s.ReadAsync(intBuf.AsMemory(0, 4));
            var unitDataLen = BinaryPrimitives.ReadUInt32LittleEndian(intBuf);
            if (unitDataLen > Checks.MaxDataBytesPerUnit) throw new Exception("maximum allowed data size in a preunit exceeded");
            var unitData = new byte[unitDataLen];
            await s.ReadAsync(unitData.AsMemory(0, (int)unitDataLen));

            await s.ReadAsync(intBuf.AsMemory(0, 4));
            var rsDataLen = BinaryPrimitives.ReadUInt32LittleEndian(intBuf);
            if (rsDataLen > Checks.MaxRandomSourceDataBytesPerUnit) throw new Exception("maximum allowed random source data size in a preunit exceeded");
            var rsData = new byte[rsDataLen];
            await s.ReadAsync(rsData.AsMemory(0, (int)rsDataLen));

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

        public async Task<IPreunit[]> DecodeChunkAsync()
        {
            var buf = new byte[4];
            await s.ReadAsync(buf.AsMemory(0, 4));
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
