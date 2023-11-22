using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AuremBlockAuthority.Network
{
    public interface IPacketBody
    {
        public void Serialize(Stream s);

        public void Deserialize(Stream s);

        public void Deserialize(ReadOnlySpan<byte> s);

        public int Size { get; }

        public uint Checksum()
        {
            using var ms = new MemoryStream(Size);
            this.Serialize(ms);
            uint _chk = BinaryPrimitives.ReadUInt32LittleEndian(SHA256.HashData(SHA256.HashData(ms.ToArray())));
        
            return _chk;
        }
    }
}
