using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Syncing.Internals.Packets
{
    public class PacketHeader
    {
        public byte NetworkID { get; set; }
        public byte PacketID { get; set; }
        public uint Length { get; set; }
        public uint Checksum { get; set; }

        public int Size => StaticSize;

        public static int StaticSize => 10;

        public static byte DefaultNetworkID => 1;

        public PacketHeader() { }

        public PacketHeader(ReadOnlySpan<byte> data)
        {
            Deserialize(data);
        }

        public PacketHeader(byte packetID, IPacketBody body)
        {
            NetworkID = DefaultNetworkID;
            PacketID = packetID;
            Length = (uint)body.Size;
            Checksum = body.Checksum();
        }

        public void Serialize(Stream s)
        {
            var uintBuf = new byte[4];

            s.WriteByte(NetworkID);
            s.WriteByte(PacketID);
            BinaryPrimitives.WriteUInt32LittleEndian(uintBuf, Length);
            s.Write(uintBuf);
            BinaryPrimitives.WriteUInt32LittleEndian(uintBuf, Checksum);
            s.Write(uintBuf);
        }

        public void Deserialize(Stream s)
        {
            NetworkID = (byte)s.ReadByte();
            PacketID = (byte)s.ReadByte();

            var uintBuf = new byte[4];

            s.Read(uintBuf);
            Length = BinaryPrimitives.ReadUInt32LittleEndian(uintBuf);

            s.Read(uintBuf);
            Checksum = BinaryPrimitives.ReadUInt32LittleEndian(uintBuf);
        }

        public void Deserialize(ReadOnlySpan<byte> s)
        {
            NetworkID = s[0];
            PacketID = s[1];
            Length = BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(2));
            Checksum = BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(6));
        }
    }
}
