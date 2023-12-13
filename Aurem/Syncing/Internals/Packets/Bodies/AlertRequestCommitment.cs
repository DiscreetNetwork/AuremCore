using Aurem.Forking;
using Aurem.Model;
using Aurem.Serialize;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Syncing.Internals.Packets.Bodies
{
    public class AlertRequestCommitment : IPacketBody
    {
        private int _size = 0;

        public Hash Hash { get; set; }

        public ushort Pid { get; set; }

        public byte Unknown { get; set; }
        
        public List<ICommitment>? DecodedComms { get; set; } = null;

        public Exception? DecodeException { get; set; } = null;

        public ICommitment? EncodedComm { get; set; } = null;

        public RmcSendFinished? Finished { get; set; } = null;

        public AlertRequestCommitment(Stream s)
        {
            Deserialize(s);
        }

        public AlertRequestCommitment(ReadOnlySpan<byte> b)
        {
            Deserialize(b);
        }

        public AlertRequestCommitment(Hash hash, ushort pid, byte code)
        {
            Pid = pid;
            Hash = hash;
            Unknown = code;
            _size = 35;
        }

        public AlertRequestCommitment(Hash hash, ushort pid, byte code, ICommitment comm, RmcSendFinished finished)
        {
            Pid = pid;
            Hash = hash;
            Unknown = code;
            EncodedComm = comm;
            Finished = finished;

            using var _ms = new MemoryStream();
            Serialize(_ms);
        }

        public int Size => _size;

        public void Deserialize(Stream s)
        {
            _size = 35;
            byte[] hash = new byte[32];
            s.Read(hash);
            Hash = new Hash(hash);

            byte[] ushrt = new byte[2];
            s.Read(ushrt);
            Pid = BinaryPrimitives.ReadUInt16LittleEndian(ushrt);

            Unknown = (byte)s.ReadByte();
            if (Unknown == 1)
            {
                return;
            }

            var _prevPos = s.Position;
            (DecodedComms, DecodeException) = ForkingUtil.AcquireCommitments(s).GetAwaiter().GetResult();
            Finished = new RmcSendFinished(s);
            _size += (int)(s.Position - _prevPos);
        }

        public void Deserialize(ReadOnlySpan<byte> s)
        {
            _size = 35;
            
            Hash = new Hash(s[0..32].ToArray());
            Pid = BinaryPrimitives.ReadUInt16LittleEndian(s[32..34]);
            Unknown = s[34];

            if (Unknown == 1)
            {
                return;
            }

            using var _ms = new MemoryStream(s[35..].ToArray());
            (DecodedComms, DecodeException) = ForkingUtil.AcquireCommitments(_ms).GetAwaiter().GetResult();
            Finished = new RmcSendFinished(_ms);
            _size += (int)_ms.Position;
        }

        public void Serialize(Stream s)
        {
            var _prevPos = s.Position;
            s.Write(Hash.Data);
            byte[] ushrt = new byte[2];
            BinaryPrimitives.WriteUInt16LittleEndian(ushrt, Pid);
            s.Write(ushrt);
            s.WriteByte(Unknown);
            s.Write(EncodedComm?.Marshal() ?? Array.Empty<byte>());
            if (Unknown != 1)
            {
                s.Write(EncodeUtil.EncodeUnit(null!));
            }
            Finished?.Serialize(s);
            _size = (int)(s.Position - _prevPos);
        }
    }
}
