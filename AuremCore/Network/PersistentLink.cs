using AuremCore.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Network
{
    /// <summary>
    /// Wraps a persistent TCP connection and distributes incoming traffic to multiple virtual connections (<see cref="Conn"/>).
    /// It comes in two variants: outgoing link (allows creating new conns with Call()) and incoming link (creates a new con upon receiving data with unknown ID and puts that conn in the listener queue).
    /// The two variants are distinguished by the existence of said queue; outgoing links have null, versus incoming having the queue instantiated.
    /// When encountering an error during reading, the link shuts down the TCP connection and marks itself as dead. 
    /// To restore communication, a new link must be created.
    /// </summary>
    public class PersistentLink
    {
        public TcpClient TcpLink;

        private IPEndPoint RemoteAddress;
        private ConcurrentDictionary<ulong, PersistentConn> Connections;
        private ConcurrentQueue<Conn> Queue;
        private ulong LastId;
        private CancellationToken Quit; // FIXME BRANDON! This needs to be cancellation token, and using a cancellation token source. 

        private WaitGroup Wg;
        private readonly object mutex = new object();

        public PersistentLink(TcpClient tcpLink, ConcurrentQueue<Conn> queue, WaitGroup waitGroup, CancellationToken quit)
        {
            TcpLink = tcpLink;
            Queue = queue;
            Connections = new ConcurrentDictionary<ulong, PersistentConn>();
            RemoteAddress = tcpLink.Client.RemoteEndPoint as IPEndPoint;
            Wg = waitGroup;
            Quit = quit;
        }

        internal void EraseConn(ulong id)
        {
            lock (mutex)
            {
                Connections.Remove(id, out _);
            }
        }

        public void Start()
        {
            Wg.Add(1);
            _ = Task.Factory.StartNew(async() =>
            {
                try
                {
                    while (!Quit.IsCancellationRequested)
                    {
                        try
                        {
                            if (TcpLink.Available > 0 && TcpLink.GetStream().DataAvailable)
                            {
                                var hdr = new byte[PersistentConn.HeaderSize];
                                var nread = await TcpLink.GetStream().ReadAsync(hdr, Quit);
                                if (nread != PersistentConn.HeaderSize)
                                {
                                    Stop();
                                    return;
                                }

                                (var id, var sz) = PersistentConn.ParseHeader(hdr);
                                (var conn, var ok) = GetConn(id);
                                if (sz == 0)
                                {
                                    if (ok) conn?.LocalClose();
                                    continue;
                                }

                                var buf = new byte[sz];
                                var numRead = 0;
                                try
                                {
                                    while (numRead < sz && !Quit.IsCancellationRequested)
                                    {
                                        var curRead = await TcpLink.GetStream().ReadAsync(buf.AsMemory(numRead));
                                        numRead += curRead;
                                        if (curRead == 0 && numRead < sz)
                                        {
                                            Stop();
                                            return;
                                        }
                                        else if (numRead >= sz) break;
                                    }
                                }
                                catch
                                {
                                    Stop();
                                    return;
                                }

                                if (ok)
                                {
                                    conn.Enqueue(buf);
                                    continue;
                                }

                                if (IsOut()) { }
                                else
                                {
                                    var nc = new PersistentConn(id, this);
                                    nc.Enqueue(buf);
                                    AddConn(nc);
                                    Queue.Enqueue(nc);
                                }
                            }
                            else
                            {
                                await Task.Delay(50, Quit);
                            }
                        }
                        catch
                        {
                            Stop();
                            return;
                        }
                    }
                }
                finally
                {
                    Wg.Done();
                }
            });
        }

        public (PersistentConn?, bool) GetConn(ulong id)
        {
            lock (mutex)
            {
                var success = Connections.TryGetValue(id, out var conn);
                return (conn, success);
            }
        }

        public void AddConn(PersistentConn c)
        {
            lock (mutex)
            {
                Connections[c.Id] = c;
            }
        }

        public bool IsOut() => Queue == null;

        public bool IsDead()
        {
            lock (mutex)
            {
                return TcpLink == null || !TcpLink.Connected;
            }
        }

        public void Stop()
        {
            lock (mutex)
            {
                if (TcpLink == null || !TcpLink.Connected) return;

                List<ulong> toRemove = new();
                foreach ((var id, var conn) in Connections)
                {
                    if (Interlocked.CompareExchange(ref conn.Closing, 1, 0) == 0)
                    {
                        conn.SendFinished();
                        conn.ConnFinalize();
                        toRemove.Add(id);
                    }
                }
                
                foreach (var id in toRemove) Connections.Remove(id, out _);

                TcpLink.Close();
                TcpLink = null;
                Connections = null;
            }
        }

        public async Task<Conn?> Call()
        {
            if (!IsOut()) return null;

            lock (mutex)
            {
                var conn = new PersistentConn(LastId, this);
                Connections[conn.Id] = conn;
                LastId++;
                return conn;
            }
        }

        public IPEndPoint RemoteAddr() => (TcpLink.Client.RemoteEndPoint is IPEndPoint ep) ? ep : throw new Exception("invalid connection");
    }
}
