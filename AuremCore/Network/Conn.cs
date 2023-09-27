using System;
using System.Buffers.Binary;
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

        //public abstract Stream NetStream { get; }

        public abstract Task Interrupt();

        public virtual async Task Connect()
        {

        }

        public abstract Task Close();

        public abstract Task<int> Read(byte[] s);

        public abstract Task<int> Write(byte[] s);

        public async Task<(int, Exception?)> TryRead(byte[] s)
        {
            try
            {
                var x = await Read(s);
                return (x, null);
            }
            catch (Exception ex)
            {
                return (0, ex);
            }
        }

        public async Task<(int, Exception?)> TryWrite(byte[] s)
        {
            try
            {
                var x = await Write(s);
                return (x, null);
            }
            catch (Exception ex)
            {
                return (0, ex);
            }
        }

        public abstract void Flush();

        public Exception? TryFlush()
        {
            try
            {
                Flush();
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        public virtual async Task Greet(ushort pid, ulong sid)
        {
            var data = new byte[10];
            BinaryPrimitives.WriteUInt16LittleEndian(data, pid);
            BinaryPrimitives.WriteUInt64LittleEndian(data.AsSpan(2), sid);
            await Write(data);
        }


        public virtual async Task<(ushort Pid, ulong Sid)> AcceptGreeting()
        {
            var data = new byte[10];
            var tot = await Read(data);
            if (tot != 10) throw new Exception($"Conn.AcceptGreeting: expected 10 bytes; got {tot}");

            var pid = BinaryPrimitives.ReadUInt16LittleEndian(data);
            var sid = BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(2));
            return (pid, sid);
        }
    }
}
