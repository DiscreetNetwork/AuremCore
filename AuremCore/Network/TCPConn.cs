using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Network
{
    public class TCPConn: Conn
    {
        private IPEndPoint _remote;
        private bool _connected;
        private CancellationTokenSource _cancellationTokenSource;
        private TimeSpan timeout = TimeSpan.FromSeconds(15);

        public override IPEndPoint RemoteEndPoint => _remote;
        public override bool IsConnected => _connected;

        //public override Stream NetStream => client?.GetStream();

        private static readonly int bufferSize = 32000;

        TcpClient client;

        public TCPConn() { }

        public TCPConn(TcpClient client)
        {
            this.client = client;
            _remote = (IPEndPoint)client?.Client?.RemoteEndPoint;
            _connected = client.Connected;
            _cancellationTokenSource = new();
        }

        public TCPConn(TcpClient client, TimeSpan timeout)
        {
            this.client = client;
            this.timeout = timeout;
            _remote = (IPEndPoint)client?.Client?.RemoteEndPoint;
            _connected = client.Connected;
            _cancellationTokenSource = new();
        }

        public override async Task Connect()
        {
            if (!client.Connected)
            {
                client.Connect(Address, Port);
                _connected = client.Connected;
            }
        }

        public override async Task<int> Read(byte[] s)
        {
            CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token);
            cts.CancelAfter(timeout);
            var res = await client.GetStream().ReadAsync(s, cts.Token);
            if (cts.IsCancellationRequested && !_cancellationTokenSource.IsCancellationRequested) throw new Exception("Timeout");

            return res;
        }

        public override async Task<int> Write(byte[] s)
        {
            CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token);
            cts.CancelAfter(timeout);
            await client.GetStream().WriteAsync(s, cts.Token);

            if (_cancellationTokenSource.IsCancellationRequested) return 0;
            if (cts.IsCancellationRequested)
            {
                // timeout
                throw new Exception("Timeout");
            }

            return s.Length;
        }

        public override async Task Close()
        {
            try
            {
                await client.GetStream().FlushAsync(_cancellationTokenSource.Token);
            }
            finally
            {
                client.Close();
            }
        }

        public override async Task Interrupt()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource = new();
        }

        public override void Flush()
        {
            client.GetStream().Flush();
        }

        public override async Task Greet(ushort pid, ulong sid)
        {
            byte[] data = new byte[10];
            BinaryPrimitives.WriteUInt16LittleEndian(data, pid);
            BinaryPrimitives.WriteUInt64LittleEndian(data.AsSpan(2), sid);
            await Write(data);
        }

        public override async Task<(ushort Pid, ulong Sid)> AcceptGreeting()
        {
            byte[] data = new byte[10];
            await Read(data);

            var pid = BinaryPrimitives.ReadUInt16LittleEndian(data);
            var sid = BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(2));
            return (pid, sid);
        }
    }
}
