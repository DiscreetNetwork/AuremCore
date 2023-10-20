using Aurem.Syncing.Internals.Packets;
using Aurem.Syncing.Internals.Packets.Bodies;
using AuremCore.Core;
using AuremCore.Core.Extensions;
using AuremCore.Crypto.P2P;
using AuremCore.FastLogger;
using AuremCore.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Syncing.Internals
{
    /// <summary>
    /// Implements a network server which actively starts all connections.
    /// </summary>
    public class Network
    {
        private TcpListener listener;
        private IPEndPoint[] remoteAddresses;
        private Connection[] connsDialed;
        private Connection[] connsListened;
        private CancellationTokenSource cancellationTokenSource;
        private Logger Log;
        private TimeSpan timeout = TimeSpan.FromSeconds(15);
        private Config.Config Conf;
        private WaitGroup taskWg;

        internal Func<Packet, Task> OnReceive = (_) => Task.CompletedTask; 

        public Network(string local, string[] remotes, Logger log, TimeSpan timeout, Config.Config conf)
        {
            IPEndPoint localp = IPEndPoint.Parse(local);
            listener = new TcpListener(localp);

            this.timeout = timeout;
            listener.Start();
            remoteAddresses = remotes.Select(x => IPEndPoint.Parse(x)).ToArray();
            cancellationTokenSource = new CancellationTokenSource();
            Log = log;
            Conf = conf;

            connsDialed = new Connection[remoteAddresses.Length];
            connsListened = new Connection[remoteAddresses.Length];
            taskWg = new WaitGroup();
        }

        public void AddHandle(Func<Packet, Task> handle)
        {
            OnReceive += handle;
        }

        // TODO: make more efficient
        public async Task<Connection> Dial(ushort pid)
        {
            if (pid == Conf.Pid) return null!;

            if (connsDialed[pid] != null) return connsDialed[pid];

            // parse the connection
            //await Console.Out.WriteLineAsync($"Dial pid={pid}, total addresses={remoteAddresses?.Length}");
            if (pid >= remoteAddresses.Length) throw new ArgumentOutOfRangeException(nameof(pid));
            var addr = remoteAddresses[pid];

            var _client = new TcpClient();
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token);
            cts.CancelAfter(timeout);

            await _client.ConnectAsync(addr, cts.Token);
            if (cts.IsCancellationRequested && !cancellationTokenSource.IsCancellationRequested)
            {
                throw new TimeoutException();
            }
            else if (cancellationTokenSource.Token.IsCancellationRequested)
            {
                return null!;
            }

            var conn = new Connection(_client, timeout, Conf.P2PSecretKey, null, this);

            var success = await conn.SendHandshake(Conf.Pid);
            
            if (success)
            {
                if (connsDialed[pid] != null) return connsDialed[pid];
                else connsDialed[pid] = conn;
            }

            return connsDialed[pid];
        }

        public async Task<Connection> Listen()
        {
            var _client = await listener.AcceptTcpClientAsync(cancellationTokenSource.Token);
            if (cancellationTokenSource.IsCancellationRequested)
            {
                return null!;
            }

            var conn = new Connection(_client, timeout, null, Conf.P2PPublicKeys, this);
            var pid = await conn.AcceptHandshake();

            //if (disregard)
            //{
            //    await conn.Disregard();
            //}
            //else
            {
                await conn.Heed();
                //connsListened[pid] = conn;
                connsListened[pid] = conn;
            }

            return connsListened[pid];
        }

        public async Task Start()
        {
            // listen
            _ = Task.Run(async () =>
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    await Listen();
                    await Task.Delay(50, cancellationTokenSource.Token);
                }
            });

            // attempt to dial all connections
            var swg = new WaitGroup();
            swg.Add(connsDialed.Length - 1);
            for (int i = 0; i < connsDialed.Length; i++)
            {
                if (i == Conf.Pid) continue;
                ushort pid = (ushort)i;
                _ = Task.Run(async () =>
                {
                    await Dial(pid);
                    swg.Done();
                });
            }

            await swg.WaitAsync();
            //await Console.Out.WriteLineAsync($"Server started for pid={Conf.Pid}");
            _ = Task.Run(async () =>
            {
                try
                {
                    await HandleNetworkActivity();
                }
                catch (Exception ex)
                {

                }
            });
        }

        public void Send(ushort pid, Packet p)
        {
            if (pid >= connsDialed.Length) throw new ArgumentOutOfRangeException(nameof(pid));
            connsDialed[pid]?.Send(p);
        }

        public void Broadcast(Packet p)
        {
            var indices = MulticastServer.ShuffleOrder((ushort)connsDialed.Length);
            foreach (var idx in indices)
            {
                if (idx == Conf.Pid) continue;
                connsDialed[idx]?.Send(p);
            }
        }

        public async Task StopAsync()
        {
            await taskWg.WaitAsync();
            cancellationTokenSource.Cancel();
            foreach (var conn in connsDialed)
            {
                conn?.Stop();
            }
        }

        private async Task HandleNetworkActivity()
        {
            while (!cancellationTokenSource.IsCancellationRequested)
            {
                var order = connsDialed.Zip(Enumerable.Repeat(true, connsDialed.Length)).Concat(connsListened.Zip(Enumerable.Repeat(false, connsListened.Length))).ToList().Shuffle();
                //var rOrW = connsDialed.GenerateRandomBytePerItem();
                foreach ((var conn, var dialed) in order)
                {
                    if (conn != null)
                    {
                        // decide whether to read or write
                        if (dialed)
                        {
                            // write
                            var success = conn.OptimisticTryGetPacket(out var p);
                            if (p != null)
                            {
                                _ = Task.Run(async () =>
                                {
                                    taskWg.Add(1);
                                    try
                                    {
                                        await conn.SendAll(p);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error().Msg(ex.Message);
                                    }
                                    finally
                                    {
                                        taskWg.Done();
                                    }
                                });
                            }
                        }
                        else
                        {
                            // read
                            if (conn.DataAvailable)
                            {
                                _ = Task.Run(async () =>
                                {
                                    taskWg.Add(1);
                                    try
                                    {
                                        await conn.ReceiveAll();
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error().Msg(ex.Message);
                                    }
                                    finally
                                    {
                                        taskWg.Done();
                                    }
                                });
                            }
                        }
                    }
                }

                await Task.Delay(10, cancellationTokenSource.Token);
            }
        }
    }
}
