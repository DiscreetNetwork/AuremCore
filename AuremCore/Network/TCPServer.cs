using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using AuremCore.FastLogger;

namespace AuremCore.Network
{
    public class TCPServer: Server
    {
        private TcpListener listener;
        private string[] remoteAddresses;
        private CancellationTokenSource cancellationTokenSource;
        private Logger Log;
        private TimeSpan timeout = TimeSpan.FromSeconds(15);
        private const bool AllowListenTimeouts = false;

        public TCPServer() { }

        public TCPServer(string local, string[] remotes, Logger log, TimeSpan timeout)
        {
            IPEndPoint localp = IPEndPoint.Parse(local);
            listener = new TcpListener(localp);

            this.timeout = timeout;
            listener.Start();
            remoteAddresses = remotes;
            cancellationTokenSource = new CancellationTokenSource();
        }

        public TCPServer(string local, string[] remotes, Logger log)
        {
            IPEndPoint localp = IPEndPoint.Parse(local);
            listener = new TcpListener(localp);

            listener.Start();
            remoteAddresses = remotes;
            cancellationTokenSource = new CancellationTokenSource();
        }

        public override async Task<Conn> Listen()
        {
            if (AllowListenTimeouts)
            {
#pragma warning disable CS0162 // Unreachable code detected (due to debug const AllowListenTimeouts)
                var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token);
                var tok = cts.Token;
                _ = Task.Run(async () =>
                {
                    await Task.Delay(timeout, cancellationTokenSource.Token);
                    if (!cancellationTokenSource.IsCancellationRequested)
                    {
                        cts.Cancel();
                    }
                });

                var _client = await listener.AcceptTcpClientAsync(tok);
                if (tok.IsCancellationRequested && !cts.IsCancellationRequested)
                {
                    throw new Exception("TcpServer.Listen: timeout");
                }

                return new TCPConn(_client, timeout);
            }
            else
            {
                var _client = await listener.AcceptTcpClientAsync();
                return new TCPConn(_client, timeout);
#pragma warning restore CS0162 // Unreachable code detected
            }
        }

        public override async Task<Conn?> Dial(ushort pid)
        {
            // parse the connection
            if (pid >= remoteAddresses.Length) throw new ArgumentOutOfRangeException();
            var addr = IPEndPoint.Parse(remoteAddresses[pid]);

            var _client = new TcpClient();
            var connres = _client.BeginConnect(addr.Address, addr.Port, null, null);
            var _timeout = timeout.Ticks;
            while (!connres.IsCompleted && _timeout > DateTime.UtcNow.Ticks && !cancellationTokenSource.Token.IsCancellationRequested) await Task.Delay(100);

            if (!connres.IsCompleted)
            {
                throw new TimeoutException();
            }
            else if (cancellationTokenSource.Token.IsCancellationRequested)
            {
                return null;
            }

            return new TCPConn(_client, timeout);
        }

        public override void Stop()
        {
            try
            {
                listener.Stop();
                cancellationTokenSource.Cancel();
            }
            catch (Exception ex)
            {
                Log.Err(ex).Msg("An error occurred while calling Close on the TcpListener");
            }
        }
    }
}
