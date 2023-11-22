using AuremCore.Core.Extensions;
using AuremCore.Crypto.P2P;
using AuremCore.Crypto.Threshold;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Random
{
    public class Vote
    {
        public SharedSecret? Proof;
        
        public Vote(SharedSecret proof)
        {
            Proof = proof;
        }

        public bool IsCorrect() => Proof == null;

        public static byte[] MarshalVotes(Vote[] votes)
        {
            using var ms = new MemoryStream();
            foreach (var v in votes)
            {
                if (v == null) ms.WriteByte(0);
                else if (v.IsCorrect()) ms.WriteByte(1);
                else
                {
                    ms.WriteByte(2);
                    var proofBytes = v.Proof!.Marshal();

                    var buf = new byte[2];
                    BinaryPrimitives.WriteUInt16LittleEndian(buf, (ushort)proofBytes.Length);
                    ms.Write(proofBytes);

                    ms.Write(proofBytes);
                }
            }

            return ms.ToArray();
        }

        public static byte[] MarshalVotes(Vote[,] votes, ushort pid)
        {
            using var ms = new MemoryStream();
            for (int i = 0; i < votes.GetLength(0); i++)
            {
                var v = votes[pid, i];
                if (v == null) ms.WriteByte(0);
                else if (v.IsCorrect()) ms.WriteByte(1);
                else
                {
                    ms.WriteByte(2);
                    var proofBytes = v.Proof!.Marshal();

                    var buf = new byte[2];
                    BinaryPrimitives.WriteUInt16LittleEndian(buf, (ushort)proofBytes.Length);
                    ms.Write(proofBytes);

                    ms.Write(proofBytes);
                }
            }

            return ms.ToArray();
        }

        public static (Vote[], Exception?) UnmarshalVotes(byte[] data, ushort nproc)
        {
            var votes = new Vote[nproc];
            if (data == null) return (null!, new Exception("data was null"));
            
            for (ushort pid = 0; pid < nproc; pid++)
            {
                if (data.Length < 1)
                {
                    return (null!, new Exception("votes wrongly encoded"));
                }

                if (data[0] == 0)
                {
                    votes[pid] = null!;
                    data = data[1..];
                }
                else if (data[0] == 1)
                {
                    data = data[1..];
                    votes[pid] = new Vote(null!);
                }
                else
                {
                    data = data[1..];
                    if (data.Length < 2)
                    {
                        return (null!, new Exception("votes wrongly encoded"));
                    }

                    var proofLen = BinaryPrimitives.ReadUInt16LittleEndian(data);
                    data = data[2..];

                    if (data.Length < proofLen)
                    {
                        return (null!, new Exception("votes wrongly encoded"));
                    }
                    var proof = DelegateExtensions.InvokeAndCaptureException(new SharedSecret().Unmarshal, data, out var err);
                    if (err != null)
                    {
                        return (null!, err);
                    }
                    data = data[proofLen..];
                    votes[pid] = new Vote(proof);
                }
            }

            return (votes, null);
        }

        public static byte[] MarshalShares(Share[] shares)
        {
            using var ms = new MemoryStream();
            foreach (var share in shares)
            {
                if (share == null) ms.WriteByte(0);
                else
                {
                    ms.WriteByte(1);
                    var shareMarhsalled = share.Marshal();
                    var buf = new byte[2];
                    BinaryPrimitives.WriteUInt16LittleEndian(buf, (ushort)shareMarhsalled.Length);
                    ms.Write(buf);
                    ms.Write(shareMarhsalled);
                }
            }

            return ms.ToArray();
        }

        public static (Share[], Exception?) UnmarshalShares(byte[] data, ushort nproc)
        {
            if (data == null) return (null!, new Exception("data was null"));

            var shares = new Share[nproc];
            for (ushort pid = 0; pid < nproc; pid++)
            {
                if (data.Length < 1)
                {
                    return (null!, new Exception("shares wrongly encoded"));
                }

                if (data[0] == 0)
                {
                    shares[pid] = null!;
                    data = data[1..];
                }
                else
                {
                    data = data[1..];
                    if (data.Length < 2)
                    {
                        return (null!, new Exception("shares wrongly encoded"));
                    }

                    var shareLen = BinaryPrimitives.ReadUInt16LittleEndian(data);
                    data = data[2..];

                    if (data.Length < shareLen)
                    {
                        return (null!, new Exception("shares wrongly encoded"));
                    }
                    var share = DelegateExtensions.InvokeAndCaptureException(new Share().Unmarshal, data, out var err);
                    if (err != null)
                    {
                        return (null!, err);
                    }
                    data = data[shareLen..];
                    shares[pid] = share;
                }
            }

            return (shares, null);
        }
    }
}
