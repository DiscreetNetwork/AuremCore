using AuremCore.Crypto.Multi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Core
{
    public class Block : Preblock
    {
        public ulong ID;
        public List<byte[]> AdditionalData;
        public MultiSignature Signature;

        public byte[] BlockHash()
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter bw = new BinaryWriter(ms);
            bw.Write(ID);
            foreach (var d in Data)
            {
                bw.Write(d);
            }
            bw.Write(RandomBytes);
            foreach (var d in AdditionalData)
            {
                bw.Write(d);
            }
            return SHA256.HashData(ms.ToArray());
        }

        public Block() { }

        public Block(Preblock pb, ulong id, IEnumerable<byte[]> additionalData)
        {
            this.Data = pb.Data;
            this.RandomBytes = pb.RandomBytes;
            this.ID = id;
            this.AdditionalData = additionalData.ToList();
        }
    }
}
