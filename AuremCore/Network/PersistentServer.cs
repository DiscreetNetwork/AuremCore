using AuremCore.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Network
{
    public class PersistentServer
    {
        private string LocalAddress;
        private List<string> RemoteAddresses;

        private PersistentLink[] Callers;
        private SemaphoreSlim[] Mx;
        private List<PersistentLink> Receivers;

        private ConcurrentQueue<Conn> Queue;
        private TcpListener Listener;
        private WaitGroup Wg;
        private CancellationTokenSource Quit;
        private TimeSpan Timeout;

        public PersistentServer(string localAddress, IEnumerable<string> remoteAddresses, TimeSpan timeout)
        {
            var nproc = remoteAddresses.ToList().Count;
            
            LocalAddress = localAddress;
            RemoteAddresses = remoteAddresses.ToList();
            Callers = new PersistentLink[nproc];
            Receivers = new(nproc);
            Mx = Enumerable.Repeat(new SemaphoreSlim(1, 1), nproc).ToArray();

            Queue = new ConcurrentQueue<Conn>();
            Quit = new();
            Wg = new();
            Timeout = timeout;
        }

        public async Task<Conn?> Dial(ushort pid)
        {
            var caller = await GetCaller(pid, Timeout);
            return await caller.Call();
        }

        public async Task<Conn> Listen()
        {
            var _timeout = DateTime.UtcNow.Add(Timeout).Ticks;
            while (Queue.IsEmpty && _timeout > DateTime.UtcNow.Ticks)
            {
                await Task.Delay(50);
            }

            var success = Queue.TryDequeue(out var conn);
            if (success) return conn;
            else throw new Exception($"PersistentServer.Listen: listen timed out.");
        }

        public void Start()
        {
            var local = IPEndPoint.Parse(LocalAddress);
            Listener = new TcpListener(local);

            _ = Task.Factory.StartNew(async () =>
            {
                Wg.Add(1);

                try
                {
                    while (!Quit.Token.IsCancellationRequested)
                    {
                        try
                        {
                            var ln = await Listener.AcceptTcpClientAsync();
                            if (ln == null)
                            {
                                continue;
                            }

                            var link = new PersistentLink(ln, Queue, Wg, Quit.Token);
                            lock (Receivers) Receivers.Add(link);
                            link.Start();
                        }
                        catch
                        {
                            await Task.Delay(100);
                            continue;
                        }
                    }
                }
                finally
                {
                    Wg.Done();
                }
            });
        }

        // TODO: the Wg stuff is basically a literal translation. This can probably be done better using async/await
        public async Task Stop()
        {
            Quit.Cancel();
            Listener.Stop();
            foreach (var link in Callers) link.Stop();
            foreach (var link in Receivers) link.Stop();
            
            await Wg.WaitAsync();
        }

        public static async Task<TcpClient> DialTimeout(TimeSpan timeout, IPEndPoint remote)
        {
            var _timeout = DateTime.UtcNow.Add(timeout).Ticks;
            var client = new TcpClient();
            AsyncCallback callback;
            CancellationTokenSource cts = new CancellationTokenSource();
            var error = false;
            callback = (x) =>
            {
                if (x.IsCompleted && client.Connected) cts.Cancel();
            };

            try
            {
                var result = client.BeginConnect(remote.Address, remote.Port, callback, null);
                await Task.Delay(timeout, cts.Token);

                if (cts.IsCancellationRequested) return client;


                if (!result.IsCompleted)
                {
                    client.EndConnect(result);
                    error = true;
                }
                else
                {
                    return client;
                }
            }
            catch (ObjectDisposedException ex)
            {
                error = true;
                throw new Exception($"PersistentServer.DialTimeout: connection with remote peer {remote} was disposed prior to completing.", ex);
            }
            catch (SecurityException ex)
            {
                error = true;
                throw new Exception($"PersistentServer.DialTimeout: unknown security error when trying to connect to remote peer {remote}.", ex);
            }
            catch (SocketException ex)
            {
                error = true;
                throw new Exception($"PersistentServer.DialTimeout: socket error encountered when trying to connect to remote peer {remote}.", ex);
            }
            catch (Exception ex)
            {
                error = true;
                throw new Exception($"PersistentServer.DialTimeout: {ex.GetType().Name} encountered when trying to connect to remote peer {remote}.", ex);
            }
            finally
            {
                if (error) client.Dispose();
            }

            throw new Exception($"PersistentServer.DialTimeout: connection with remote peer {remote} could not be completed.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        /// <exception cref="Exception">Throws an exception on error.</exception>
        public async Task<PersistentLink> GetCaller(ushort pid, TimeSpan timeout)
        {
            if (pid >= Mx.Length) throw new Exception("invalid pid");

            try
            {
                await Mx[pid].WaitAsync();
                
                if (Callers[pid] == null || Callers[pid].IsDead())
                {
                    var client = await DialTimeout(timeout, IPEndPoint.Parse(RemoteAddresses[pid]));
                    var link = new PersistentLink(client, null, Wg, Quit.Token);

                    Callers[pid] = link;
                    link.Start();
                }

                return Callers[pid];
            }
            finally
            {
                Mx[pid].Release();
            }
        }
    }
}
