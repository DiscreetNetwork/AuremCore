using Aurem.Model;
using Aurem.Serialize;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Syncing.Internals.Packets.Bodies
{
    public class GossipGreetPacket : IPacketBody
    {
        public ushort Pid { get; private set; }
        public uint Sid { get; private set; }
        public DagInfo[] DagInfo { get; private set; }

        public int Size
        {
            get
            {
                if (DagInfo == null || DagInfo.Length < 2) return 14;

                var rv = 6;
                rv += (DagInfo[0] == null) ? 4 : (6 + DagInfo[0].Heights.Length * 4);
                rv += (DagInfo[1] == null) ? 4 : (6 + DagInfo[1].Heights.Length * 4);

                return rv;
            }
        }

        public GossipGreetPacket(ushort pid, uint sid, DagInfo[] dagInfo)
        {
            Pid = pid;
            Sid = sid;
            DagInfo = dagInfo;
        }

        public GossipGreetPacket(ReadOnlySpan<byte> data)
        {
            Deserialize(data);
        }

        public GossipGreetPacket(Stream s)
        {
            Deserialize(s);
        }

        public void Deserialize(Stream s)
        {
            var buf = new byte[4];
            s.Read(buf, 0, 2);
            Pid = BinaryPrimitives.ReadUInt16LittleEndian(buf);

            s.Read(buf);
            Sid = BinaryPrimitives.ReadUInt32LittleEndian(buf);

            DagInfo = EncodeUtil.ReadDagInfos(s);
        }

        public void Deserialize(ReadOnlySpan<byte> s)
        {
            Pid = BinaryPrimitives.ReadUInt16LittleEndian(s);
            Sid = BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(2));

            (DagInfo, _) = EncodeUtil.ReadDagInfos(s.Slice(6));
        }

        public void Serialize(Stream s)
        {
            var buf = new byte[4];
            BinaryPrimitives.WriteUInt16LittleEndian(buf, Pid);
            s.Write(buf, 0, 2);

            BinaryPrimitives.WriteUInt32LittleEndian(buf, Sid);
            s.Write(buf);

            EncodeUtil.WriteDagInfos(DagInfo, s);
        }
    }
}
