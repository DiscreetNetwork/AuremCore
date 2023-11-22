using AuremCore.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.RMC
{
    public static class Greetings
    {
        public static async Task Greet(Conn conn, ushort pid, ulong id, byte msgType)
        {
            await conn.Greet(pid, id);
            await conn.Write(new byte[] { msgType });
        }

        public static async Task<(ushort Pid, ulong Id, byte MsgType)> AcceptGreeting(Conn conn)
        {
            (var pid, var id) = await conn.AcceptGreeting();
            
            var data = new byte[1];
            await conn.Read(data);

            return (pid, id, data[0]);
        }
    }
}
