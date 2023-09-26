using AuremCore.Network;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Syncing
{
    public static class Handshakes
    {
        public static async Task<Exception?> Greet(Conn conn, ushort pid, uint sid)
        {
            var data = new byte[6];
            BinaryPrimitives.WriteUInt16LittleEndian(data, pid);
            BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(2), sid);
            (_, var err) = await conn.TryWrite(data);

            return err;
        }

        public static async Task<(ushort, uint, Exception?)> AcceptGreeting(Conn conn)
        {
            var data = new byte[6];
            (_, var err) = await conn.TryRead(data);
            if (err != null)
            {
                return (0, 0, err);
            }

            var pid = BinaryPrimitives.ReadUInt16LittleEndian(data);
            var sid = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(2));
            return (pid, sid, null);
        }
    }
}
