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
        public virtual async Task<Conn> Dial(ushort pid)
        {
            throw new NotImplementedException();
        }

        public virtual async Task<Conn> Listen()
        {
            throw new NotImplementedException();
        }

        public abstract void Stop();
    }
}
