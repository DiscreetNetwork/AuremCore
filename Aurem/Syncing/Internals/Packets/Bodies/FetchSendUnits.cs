using Aurem.Model;
using Aurem.Serialize;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aurem.Syncing.Internals.Packets.Bodies
{
    public class FetchSendUnits : IPacketBody
    {
        public ushort Pid { get; private set; }
        public uint Sid { get; private set; }

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
                    sz = (int)ms.Position + 6;
                }

                return sz;
            }
        }

        public FetchSendUnits(ushort pid, uint sid, IPreunit[] units)
        {
            Pid = pid;
            Sid = sid;
            _preunits = units;
        }

        public FetchSendUnits(ushort pid, uint sid, IUnit[] units)
        {
            Pid = pid;
            Sid = sid;
            _units = units;
            _preunits = units;
        }

        public FetchSendUnits(ReadOnlySpan<byte> data)
        {
            Deserialize(data);
        }

        public FetchSendUnits(Stream s)
        {
            Deserialize(s);
        }

        public void Deserialize(ReadOnlySpan<byte> data)
        {
            Pid = BinaryPrimitives.ReadUInt16LittleEndian(data);
            Sid = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(2));
            (_preunits, var t) = Decoder.DeserializeChunk(data.Slice(6));
            sz = t + 6;
        }

        public void Deserialize(Stream s)
        {
            var sb = s.Position;

            var buf = new byte[4];
            s.Read(buf, 0, 2);
            Pid = BinaryPrimitives.ReadUInt16LittleEndian(buf);

            s.Read(buf);
            Sid = BinaryPrimitives.ReadUInt32LittleEndian(buf);

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

            EncodeUtil.WriteChunk(_units, s);
            sz = (int)(s.Position - sb);
        }
    }
}
