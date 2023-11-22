using Aurem.Syncing.Internals.Packets.Bodies;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Syncing.Internals.Packets
{
    public class Packet
    {
        public PacketHeader Header { get; protected set; }

        public IPacketBody Body { get; protected set; }

        public int Size => Header.Size + Body.Size;

        public Packet() { }

        public Packet(ReadOnlySpan<byte> data)
        {
            Deserialize(data);
        }

        public Packet(PacketHeader header, IPacketBody body)
        {
            Header = header;
            Body = body;
        }

        public Packet(byte type, IPacketBody body)
        {
            Header = new PacketHeader(type, body);
            Body = body;
        }

        public Packet(PacketID type, IPacketBody body) : this((byte)type, body)
        {
        }

        public byte Type() => Header.PacketID;

        public void Serialize(Stream s)
        {
            Header.Serialize(s);
            Body.Serialize(s);
        }

        public void Deserialize(Stream s)
        {
            Header.Deserialize(s);
            Body = DecodePacketBody((PacketID)Header.PacketID, s);
        }

        public void Deserialize(ReadOnlySpan<byte> data)
        {
            Header.Deserialize(data);
            Body = DecodePacketBody((PacketID)Header.PacketID, data);
        }

        public static IPacketBody DecodePacketBody(PacketID t, Stream s)
        {
            return t switch
            {
                PacketID.GOSSIPGREET => new GossipGreetPacket(s),
                PacketID.GOSSIPINFO => new GossipInfoPacket(s),
                PacketID.GOSSIPUNITS => new GossipUnitsPacket(s),
                PacketID.MCASTSEND => new MCastSendUnit(s),
                PacketID.FETCHREQUEST => new FetchRequestUnits(s),
                PacketID.FETCHRESPONSE => new FetchSendUnits(s),
                PacketID.RmcData => new RmcData(s),
                PacketID.RmcProof => new RmcProof(s),
                PacketID.RmcFinished => new RmcSendFinished(s),
                PacketID.RmcSendData => new RmcSendData(s),
                PacketID.RmcSendProof => new RmcSendProof(s),
                PacketID.RmcSendFinished => new RmcSendFinished(s),
                PacketID.RmcGreet => new RmcGreet(s),
                PacketID.RmcSignature => new RmcSignature(s),
                PacketID.NONE => throw new Exception("decoding packet body revealed packet of NONE type"),
                _ => throw new Exception($"unimplemented or unknown packet type {t}")
            };
        }

        public static IPacketBody DecodePacketBody(PacketID t, ReadOnlySpan<byte> s)
        {
            return t switch
            {
                PacketID.GOSSIPGREET => new GossipGreetPacket(s),
                PacketID.GOSSIPINFO => new GossipInfoPacket(s),
                PacketID.GOSSIPUNITS => new GossipUnitsPacket(s),
                PacketID.MCASTSEND => new MCastSendUnit(s),
                PacketID.FETCHREQUEST => new FetchRequestUnits(s),
                PacketID.FETCHRESPONSE => new FetchSendUnits(s),
                PacketID.RmcData => new RmcData(s),
                PacketID.RmcProof => new RmcProof(s),
                PacketID.RmcFinished => new RmcSendFinished(s),
                PacketID.RmcSendData => new RmcSendData(s),
                PacketID.RmcSendProof => new RmcSendProof(s),
                PacketID.RmcSendFinished => new RmcSendFinished(s),
                PacketID.RmcGreet => new RmcGreet(s),
                PacketID.RmcSignature => new RmcSignature(s),
                PacketID.NONE => throw new Exception("decoding packet body revealed packet of NONE type"),
                _ => throw new Exception($"unimplemented or unknown packet type {t}")
            };
        }
    }
}
