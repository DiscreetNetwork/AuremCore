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

        public override IPEndPoint RemoteEndPoint => _remote;
        public override bool IsConnected => _connected;
        
        private static readonly int bufferSize = 32000;

        TcpClient client;

        public TCPConn() { }

        public TCPConn(TcpClient client)
        {
            this.client = client;
            _remote = (IPEndPoint)client.Client.RemoteEndPoint;
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
            return await client.GetStream().ReadAsync(s, _cancellationTokenSource.Token);
        }

        public override async Task<int> Write(byte[] s)
        {
            await client.GetStream().WriteAsync(s, _cancellationTokenSource.Token);
            return s.Length;
        }

        public override async Task Disconnect()
        {
            await client.GetStream().FlushAsync(_cancellationTokenSource.Token);
            client.Close();
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
