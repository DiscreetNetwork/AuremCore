using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Syncing.Internals.Packets.Bodies
{
    public class RmcSendData : IPacketBody
    {
        public RmcGreet Greet { get; set; }
        public RmcData Data { get; set; }

        public int Size => Greet.Size + Data.Size;

        public RmcSendData(RmcGreet greet, RmcData data)
        {
            this.Greet = greet;
            this.Data = data;
        }

        public RmcSendData(ushort pid, ulong id, byte msg, byte[] data)
        {
            Greet = new RmcGreet(pid, id, msg);
            Data = new RmcData(data);
        }

        public RmcSendData(Stream s)
        {
            Deserialize(s);
        }

        public RmcSendData(ReadOnlySpan<byte> s)
        {
            Deserialize(s);
        }

        public void Serialize(Stream s)
        {
            Greet.Serialize(s);
            Data.Serialize(s);
        }

        public void Deserialize(Stream s)
        {
            Greet = new RmcGreet(s);
            Data = new RmcData(s);
        }

        public void Deserialize(ReadOnlySpan<byte> s)
        {
            Greet = new RmcGreet(s);
            s = s.Slice(Greet.Size);
            Data = new RmcData();
            Data.Deserialize(s);
        }
    }
}
