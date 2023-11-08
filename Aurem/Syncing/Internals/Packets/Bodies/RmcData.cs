using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Syncing.Internals.Packets.Bodies
{
    public class RmcData : IPacketBody
    {
        public int Length => Data?.Length ?? 0;
        public byte[] Data { get; set; }

        public RmcData(byte[] data)
        {
            Data = data;
        }

        public RmcData(ReadOnlySpan<byte> data)
        {
            Deserialize(data);
        }

        public RmcData()
        {

        }

        public RmcData(Stream s)
        {
            Deserialize(s);
        }

        public int Size => 4 + Length;

        public void Deserialize(Stream s)
        {
            var lenBuf = new byte[4];
            s.Read(lenBuf);

            var len = BinaryPrimitives.ReadUInt32LittleEndian(lenBuf);
            Data = new byte[len];
            s.Read(Data);
        }

        public void Deserialize(ReadOnlySpan<byte> s)
        {
            var len = BinaryPrimitives.ReadUInt32LittleEndian(s);
            Data = s.Slice(4, (int)len).ToArray();
        }

        public void Serialize(Stream s)
        {
            var lenBuf = new byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(lenBuf, (uint)Length);
            s.Write(lenBuf);
            s.Write(Data);
        }
    }
}
