using Aurem.Model;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Syncing.Internals.Packets.Bodies
{
    public class RequestComm : IPacketBody
    {
        public RmcGreet Greet { get; set; }

        public Hash Hash { get; set; }

        public RequestComm(Stream s)
        {
            Deserialize(s);
        }

        public RequestComm(ReadOnlySpan<byte> b)
        {
            Deserialize(b);
        }

        public RequestComm(RmcGreet greet, Hash hash)
        {
            Greet = greet;
            Hash = hash;
        }

        public RequestComm(ulong id, ushort pid, byte msgtype, Hash hash)
        {
            Greet = new RmcGreet(pid, id, msgtype);
            Hash = hash;
        }

        public int Size => 32 + 11;

        public void Deserialize(Stream s)
        {
            Greet.Deserialize(s);

            byte[] hash = new byte[32];
            s.Read(hash);
            Hash = new Hash(hash);
        }

        public void Deserialize(ReadOnlySpan<byte> s)
        {
            Greet.Deserialize(s);

            s = s[Greet.Size..];

            Hash = new Hash(s[0..32].ToArray());
        }

        public void Serialize(Stream s)
        {
            Greet.Serialize(s);
            s.Write(Hash.Data);
        }
    }
}
