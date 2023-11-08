using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Syncing.Internals.Packets.Bodies
{
    public class RmcSendProof : IPacketBody
    {
        public RmcGreet Greet { get; set; }
        public RmcProof Proof { get; set; }

        public int Size => Greet.Size + Proof.Size;

        public RmcSendProof(RmcGreet greet, RmcProof proof)
        {
            Greet = greet;
            Proof = proof;
        }

        public RmcSendProof(ushort pid, ulong id, byte msg, byte[] proof)
        {
            Greet = new RmcGreet(pid, id, msg);
            Proof = new RmcProof(proof);
        }

        public RmcSendProof(Stream s)
        {
            Deserialize(s);
        }

        public RmcSendProof(ReadOnlySpan<byte> s)
        {
            Deserialize(s);
        }

        public void Serialize(Stream s)
        {
            Greet.Serialize(s);
            Proof.Serialize(s);
        }

        public void Deserialize(Stream s)
        {
            Greet = new RmcGreet(s);
            Proof = new RmcProof(s);
        }

        public void Deserialize(ReadOnlySpan<byte> s)
        {
            Greet = new RmcGreet(s);
            Proof = new RmcProof();
            s = s.Slice(Greet.Size);
            Proof.Deserialize(s);
        }
    }
}
