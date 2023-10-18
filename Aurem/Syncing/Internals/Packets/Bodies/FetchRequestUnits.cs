using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Syncing.Internals.Packets.Bodies
{
    public class FetchRequestUnits : IPacketBody
    {
        public ushort Pid { get; private set; }
        public uint Sid { get; private set; }
        public ulong[] UnitIDs { get; private set; }

        public int Size => 10 + (UnitIDs == null ? 0 : UnitIDs.Length * 8);

        public FetchRequestUnits(ushort pid, uint sid, ulong[] unitIDs)
        {
            Pid = pid;
            Sid = sid;
            UnitIDs = unitIDs;
        }

        public FetchRequestUnits(ReadOnlySpan<byte> bytes)
        {
            Deserialize(bytes);
        }

        public FetchRequestUnits(Stream s)
        {
            Deserialize(s);
        }

        public void Deserialize(ReadOnlySpan<byte> bytes)
        {
            Pid = BinaryPrimitives.ReadUInt16LittleEndian(bytes);
            Sid = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(2));

            var len = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(6));
            UnitIDs = new ulong[len];
            bytes = bytes.Slice(10);
            for (int i = 0; i < len; i++)
            {
                UnitIDs[i] = BinaryPrimitives.ReadUInt64LittleEndian(bytes);
                bytes = bytes.Slice(8);
            }
        }

        public void Deserialize(Stream s)
        {
            byte[] buf = new byte[8];
            
            s.Read(buf.AsSpan(0, 2));
            Pid = BinaryPrimitives.ReadUInt16LittleEndian(buf);

            s.Read(buf.AsSpan(0, 4));
            Sid = BinaryPrimitives.ReadUInt32LittleEndian(buf);

            s.Read(buf.AsSpan(0, 4));
            var len = BinaryPrimitives.ReadUInt32LittleEndian(buf);
            UnitIDs = new ulong[len];
            for (int i = 0; i < len; i++)
            {
                s.Read(buf);
                UnitIDs[i] = BinaryPrimitives.ReadUInt64LittleEndian(buf);
            }
        }

        public void Serialize(Stream s)
        {
            byte[] buf = new byte[8];

            BinaryPrimitives.WriteUInt16LittleEndian(buf, Pid);
            s.Write(buf.AsSpan(0, 2));

            BinaryPrimitives.WriteUInt32LittleEndian(buf, Sid);
            s.Write(buf.AsSpan(0, 4));

            BinaryPrimitives.WriteUInt32LittleEndian(buf, (uint)(UnitIDs == null ? 0 : UnitIDs.Length));
            s.Write(buf.AsSpan(0, 4));
            if (UnitIDs == null) return;

            for (int i = 0; i < UnitIDs.Length; i++)
            {
                BinaryPrimitives.WriteUInt64LittleEndian(buf, UnitIDs[i]);
                s.Write(buf);
            }
        }
    }
}
