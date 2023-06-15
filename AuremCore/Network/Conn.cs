using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Network
{
    public abstract class Conn
    {
        public abstract IPEndPoint RemoteEndPoint { get; }

        public int Port { get
            {
                if (RemoteEndPoint == null) return -1;
                else return RemoteEndPoint.Port;
            } }

        public IPAddress? Address { get => RemoteEndPoint?.Address; }

        public abstract bool IsConnected { get; }

        public virtual async Task Interrupt()
        {
            throw new NotImplementedException();
        }

        public virtual async Task Connect()
        {
            throw new NotImplementedException();
        }

        public virtual async Task Disconnect()
        {
            throw new NotImplementedException();
        }

        public virtual async Task<int> Read(byte[] s)
        {
            throw new NotImplementedException();
        }

        public virtual async Task<int> Write(byte[] s)
        {
            throw new NotImplementedException();
        }

        public virtual async Task Greet(ushort pid, ulong sid)
        {
            throw new NotImplementedException();
        }


        public virtual async Task<(ushort Pid, ulong Sid)> AcceptGreeting()
        {
            throw new NotImplementedException();
        }

        public abstract void Flush();
    }
}
