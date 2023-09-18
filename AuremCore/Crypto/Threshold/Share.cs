using BN256Core;
using AuremCore.Crypto.P2P;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Crypto.Threshold
{
    public class Share
    {
        internal ushort owner;
        internal Signature sig;

        public ushort Owner => owner;

        public byte[] Marshal()
        {
            byte[] res = new byte[2 + Constants.SignatureLength];
            BinaryPrimitives.WriteUInt16LittleEndian(res, owner);
            Array.Copy(sig.Marshal(), 0, res, 2, Constants.SignatureLength);
            return res;
        }

        public Share Unmarshal(byte[] data)
        {
            if (data == null || data.Length < 2 + Constants.SignatureLength) throw new Exception("data is too short");

            owner = BinaryPrimitives.ReadUInt16LittleEndian(data);
            sig = new Signature().Unmarshal(data[2..]);

            return this;
        }

        public Share Unmarshal(ReadOnlySpan<byte> data)
        {
            if (data == null || data.Length < 2 + Constants.SignatureLength) throw new Exception("data is too short");

            owner = BinaryPrimitives.ReadUInt16LittleEndian(data);
            sig = new Signature().Unmarshal(data[2..]);

            return this;
        }

        public static Share SumShares(IList<Share> shs)
        {
            return new Share { owner = shs[0].Owner, sig = shs.Aggregate(new Signature(), (x, y) => Signature.Add(x, y.sig)) };
        }
    }
}
