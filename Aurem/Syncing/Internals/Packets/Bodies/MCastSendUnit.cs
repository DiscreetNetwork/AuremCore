using Aurem.Model;
using Aurem.Serialize;
using Aurem.Units;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aurem.Syncing.Internals.Packets.Bodies
{
    public class MCastSendUnit : IPacketBody
    {
        private IPreunit _preunit;
        private IUnit _unit;

        private int sz = -1;

        public int Size
        {
            get
            {
                if (sz == -1)
                {
                    sz = Encoder.SerializeUnit(_preunit).Length;
                }

                return sz;
            }
        }

        public IPreunit Unit { get { return _preunit; } }

        public MCastSendUnit(IUnit unit)
        {
            _unit = unit;
            _preunit = unit;
        }

        public MCastSendUnit(ReadOnlySpan<byte> bytes)
        {
            Deserialize(bytes);
        }

        public MCastSendUnit(Stream s)
        {
            Deserialize(s);
        }

        public void Deserialize(ReadOnlySpan<byte> bytes)
        {
            (_preunit, sz) = Decoder.DeserializePreunit(bytes);
        }

        public void Deserialize(Stream s)
        {
            var sb = s.Position;
            _preunit = EncodeUtil.ReadPreunit(s);
            sz = (int)(s.Position - sb);
        }

        public void Serialize(Stream s)
        {
            var sb = s.Position;
            EncodeUtil.WriteUnit(_unit, s);
            sz = (int)(s.Position - sb);
        }
    }
}
