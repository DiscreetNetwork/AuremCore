using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Syncing.Internals.Packets.Bodies
{
    public class RmcSignature : IPacketBody
    {
        public ushort Pid { get; set; }
        public ulong Id { get; set; }

        public byte[] Sig { get; set; }

        public RmcSignature(ushort pid, ulong id, byte[] data)
        {
            Pid = pid;
            Id = id;
            Sig = data;
        }

        public RmcSignature(ReadOnlySpan<byte> s)
        {
            Deserialize(s);
        }

        public RmcSignature(Stream s)
        {
            Deserialize(s);
        }

        public int Size => 74;

        public void Deserialize(Stream s)
        {
            var bytes = new byte[74];
            s.Read(bytes);
            Deserialize(bytes);
        }

        public void Deserialize(ReadOnlySpan<byte> s)
        {
            Pid = BinaryPrimitives.ReadUInt16LittleEndian(s);
            Id = BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(2));
            Sig = s.Slice(10, 64).ToArray();
        }

        public void Serialize(Stream s)
        {
            byte[] dbytes = new byte[10];
            BinaryPrimitives.WriteUInt16LittleEndian(dbytes, Pid);
            BinaryPrimitives.WriteUInt64LittleEndian(dbytes.AsSpan(2), Id);
            s.Write(dbytes);
            s.Write(Sig);
        }
    }
}
