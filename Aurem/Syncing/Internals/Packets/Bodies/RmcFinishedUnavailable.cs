using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Syncing.Internals.Packets.Bodies
{
    public class RmcFinishedUnavailable : IPacketBody
    {
        public RmcGreet Greet { get; set; }

        public RmcFinishedUnavailable(ushort pid, ulong id, byte msgtype)
        {
            Greet = new RmcGreet(pid, id, msgtype);
        }

        public RmcFinishedUnavailable(Stream s)
        {
            Deserialize(s);
        }

        public RmcFinishedUnavailable(ReadOnlySpan<byte> s)
        {
            Deserialize(s);
        }

        public int Size => Greet.Size;

        public void Deserialize(Stream s)
        {
            Greet ??= new(s);
        }

        public void Deserialize(ReadOnlySpan<byte> s)
        {
            Greet ??= new(s);
        }

        public void Serialize(Stream s)
        {
            Greet.Serialize(s);
        }
    }
}
