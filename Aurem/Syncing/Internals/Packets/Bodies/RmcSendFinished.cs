using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Syncing.Internals.Packets.Bodies
{
    public class RmcSendFinished : IPacketBody
    {
        public RmcSendData SendData { get; set; }
        public RmcProof Proof { get; set; }

        public int Size => SendData.Size + Proof.Size;

        public RmcSendFinished(RmcSendData sendData, RmcProof proof)
        {
            SendData = sendData;
            Proof = proof;
        }

        public RmcSendFinished(ushort pid, ulong id, byte msg, byte[] data, byte[] proof)
        {
            SendData = new RmcSendData(pid, id, msg, data);
            Proof = new RmcProof(proof);
        }

        public RmcSendFinished(Stream s)
        {
            Deserialize(s);
        }

        public RmcSendFinished(ReadOnlySpan<byte> s)
        {
            Deserialize(s);
        }

        public void Serialize(Stream s)
        {
            SendData.Serialize(s);
            Proof.Serialize(s);
        }

        public void Deserialize(Stream s)
        {
            SendData = new RmcSendData(s);
            Proof = new RmcProof(s);
        }

        public void Deserialize(ReadOnlySpan<byte> s)
        {
            SendData = new RmcSendData(s);
            s = s.Slice(SendData.Size);
            Proof = new RmcProof();
            Proof.Deserialize(s);
        }
    }
}
