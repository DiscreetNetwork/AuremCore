using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Network
{
    public abstract class Server
    {
        public abstract Task<Conn?> Dial(ushort pid);

        public abstract Task<Conn> Listen();

        public abstract void Stop();

        public async Task<(Conn?, Exception?)> TryDial(ushort pid)
        {
            try
            {
                var conn = await Dial(pid);
                return (conn, null);
            }
            catch (Exception ex)
            {
                return (null, ex);
            }
        }
    }
}
