using Aurem.Model;
using Aurem.Serialize;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Aurem.Syncing.Internals.Packets.Bodies
{
    public class GossipInfoPacket : IPacketBody
    {
        public ushort Pid { get; private set; }
        public uint Sid { get; private set; }

        public DagInfo[] DagInfo { get; private set; }
        public IPreunit[] Units { get { return _preunits; } }

        private IPreunit[] _preunits;
        private IUnit[] _units;

        private int sz = -1;

        public int Size
        {
            get
            {
                if (sz == -1)
                {
                    using var ms = new MemoryStream();
                    Serialize(ms);
                    sz = (int)ms.Position;
                }

                return sz;
            }
        }

        public GossipInfoPacket(ushort pid, uint sid, DagInfo[] dagInfo, IPreunit[] units)
        {
            Pid = pid;
            Sid = sid;
            DagInfo = dagInfo;
            _preunits = units;
        }

        public GossipInfoPacket(ushort pid, uint sid, DagInfo[] dagInfo, IUnit[] units)
        {
            Pid = pid;
            Sid = sid;
            DagInfo = dagInfo;
            _units = units;
            _preunits = units;

            //Console.WriteLine($"GOSSIPINFOPACKET: sz={Size}");
        }

        public GossipInfoPacket(ReadOnlySpan<byte> data)
        {
            Deserialize(data);
        }

        public GossipInfoPacket(Stream s)
        {
            Deserialize(s);
        }

        public void Deserialize(ReadOnlySpan<byte> data)
        {
            Pid = BinaryPrimitives.ReadUInt16LittleEndian(data);
            Sid = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(2));
            data = data.Slice(6);

            (DagInfo, var t) = EncodeUtil.ReadDagInfos(data);
            data = data.Slice(t);
            (_preunits, var t2) = Decoder.DeserializeChunk(data);
            sz = t + t2 + 6;
        }

        public void Deserialize(Stream s)
        {
            var sb = s.Position;

            var buf = new byte[4];
            s.Read(buf, 0, 2);
            Pid = BinaryPrimitives.ReadUInt16LittleEndian(buf);

            s.Read(buf);
            Sid = BinaryPrimitives.ReadUInt32LittleEndian(buf);

            DagInfo = EncodeUtil.ReadDagInfos(s);
            _preunits = EncodeUtil.ReadChunk(s);
            sz = (int)(s.Position - sb);
        }

        public void Serialize(Stream s)
        {
            var sb = s.Position;

            var buf = new byte[4];
            BinaryPrimitives.WriteUInt16LittleEndian(buf, Pid);
            s.Write(buf, 0, 2);

            BinaryPrimitives.WriteUInt32LittleEndian(buf, Sid);
            s.Write(buf);

            EncodeUtil.WriteDagInfos(DagInfo, s);
            EncodeUtil.WriteChunk(_units, s);
            sz = (int)(s.Position - sb);
        }
    }
}
