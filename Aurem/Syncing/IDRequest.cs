using Aurem.Config;
using Aurem.Model;
using AuremCore.Network;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Syncing
{
    public class IDRequest
    {
        public ushort Pid;
        public ulong[] UnitIDs;

        public byte[] Serialize()
        {
            if (UnitIDs != null && UnitIDs.Length > Checks.MaxUnitsInChunk)
            {
                UnitIDs = UnitIDs[0..Checks.MaxUnitsInChunk];
            }

            if (UnitIDs == null) return new byte[4];

            var rv = new byte[4 + (UnitIDs?.Length * 8) ?? 0];
            BinaryPrimitives.WriteUInt32LittleEndian(rv, (uint)UnitIDs!.Length);

            var offset = 4;
            foreach (var id in UnitIDs)
            {
                BinaryPrimitives.WriteUInt64LittleEndian(rv.AsSpan(offset), id);
                offset += 8;
            }

            return rv;
        }

        public static async Task<Exception?> SendRequests(Conn conn, ulong[] unitIDs)
        {
            try
            {
                await conn.NetStream.WriteAsync(new IDRequest { UnitIDs = unitIDs }.Serialize());
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        public (ulong[], Exception?) Deserialize(ReadOnlySpan<byte> data)
        {
            if (data == null) return (Array.Empty<ulong>(), new Exception("data was null"));

            if (data.Length < 4) return (Array.Empty<ulong>(), new Exception("data was invalid"));

            var nReqs = BinaryPrimitives.ReadUInt32LittleEndian(data);
            if (nReqs > Checks.MaxUnitsInChunk) return (Array.Empty<ulong>(), new Exception("requests too big"));
            if (data.Length != 4 + nReqs * 8) return (Array.Empty<ulong>(), new Exception("data was invalid"));

            var rv = new ulong[nReqs];
            for (int i = 0; i < nReqs; i++)
            {
                rv[i] = BinaryPrimitives.ReadUInt64LittleEndian(data[(4 + 8 * i)..]);
            }

            return (rv, null);
        }

        public static async Task<(ulong[], Exception?)> ReceiveRequests(Conn conn)
        {
            var lenBuf = new byte[4];
            (_, var err) = await conn.TryRead(lenBuf);
            if (err != null)
            {
                return (null!, err);
            }

            var nReqs = BinaryPrimitives.ReadUInt32LittleEndian(lenBuf);

            var buf = new byte[8 * nReqs];
            (_, err) = await conn.TryRead(buf);
            if (err != null)
            {
                return (null!, err);
            }

            var rv = new ulong[nReqs];
            for (int i = 0; i < nReqs; i++)
            {
                rv[i] = BinaryPrimitives.ReadUInt64LittleEndian(buf.AsSpan(8 * i));
            }

            return (rv, null);
        }
    }
}
