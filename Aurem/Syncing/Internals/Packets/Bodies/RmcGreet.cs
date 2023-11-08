using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Syncing.Internals.Packets.Bodies
{
    public class RmcGreet : IPacketBody
    {
        public ushort Pid { get; set; }
        public ulong Id { get; set; }
        public byte MsgType { get; set; }

        public RmcGreet(ushort pid, ulong id, byte msgtype)
        {
            Pid = pid;
            Id = id;
            MsgType = msgtype;
        }

        public RmcGreet(Stream s)
        {
            Deserialize(s);
        }

        public RmcGreet(ReadOnlySpan<byte> s)
        {
            Deserialize(s);
        }

        public int Size => 11;

        public void Deserialize(Stream s)
        {
            byte[] buf = new byte[Size];
            s.Read(buf);
            Deserialize(buf);
        }

        public void Deserialize(ReadOnlySpan<byte> s)
        {
            Pid = BinaryPrimitives.ReadUInt16LittleEndian(s);
            Id = BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(2));
            MsgType = s[10];
        }

        public void Serialize(Stream s)
        {
            var buf = new byte[11];
            BinaryPrimitives.WriteUInt16LittleEndian(buf, Pid);
            BinaryPrimitives.WriteUInt64LittleEndian(buf.AsSpan(2), Id);
            buf[10] = MsgType;
            s.Write(buf);
        }
    }
}
