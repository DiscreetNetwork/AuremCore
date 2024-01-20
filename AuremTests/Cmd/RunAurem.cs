using Aurem.Config;
using AuremCore;
using AuremCore.Core;
using AuremCore.Core.Extensions;
using AuremCore.Tests;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace AuremTests.Cmd
{
    public static class RunAurem
    {
        public class IPAddressEqualityComparer : IEqualityComparer<IPAddress>
        {
            public bool Equals(IPAddress? x, IPAddress? y)
            {
                if (x == null && y == null) return true;
                if (x == null) return false;
                return x.Equals(y);
            }

            public int GetHashCode([DisallowNull] IPAddress obj)
            {
                return obj.GetHashCode();
            }
        }

        public static (Member, Exception?) GetMember(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                return (null!, new Exception("please provide a file with private keys and pid"));
            }

            try
            {
                var f = File.OpenRead(filename);
                return (Member.LoadMember(f), null);
            }
            catch (Exception e)
            {
                return (null!, e);
            }
        }

        public static (Committee, Exception?) GetCommittee(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                return (null!, new Exception("please provide a file with keys and addresses of the committee"));
            }

            try
            {
                var f = File.OpenRead(filename);
                return (Committee.LoadCommittee(f), null);
            }
            catch (Exception e)
            {
                return (null!, e);
            }
        }

        public sealed class AuremSettings
        {
            public string PrivFilename { get; set; } = "";

            public string KeysAddrsFilename { get; set; } = "";

            public int Epochs { get; set; } = 0;

            public int Units { get; set; } = 0;

            public int Output { get; set; } = 1;

            public bool Setup { get; set; } = true;

            public long Delay { get; set; } = 0;

            public int RandomBytesPerUnit { get; set; } = 300;

            public bool UseLocalServer { get; set; } = false;

            public bool UseBlockScheduler { get; set; } = false;

            public TimeSpan BlockSchedule { get; set; } = TimeSpan.FromSeconds(1);

            public bool WaitForNodes { get; set; } = false;

            public bool Local { get; set; } = false;

            public int Sessions { get; set; } = 1;
        }

        public static async Task Run(AuremSettings settings)
        {
            if (settings.Delay > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(settings.Delay));
            }

            await Console.Out.WriteLineAsync("Starting process.");

            (var member, var err) = GetMember(settings.PrivFilename);
            if (err != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                await Console.Out.WriteLineAsync($"Invalid private key file \"{settings.PrivFilename}\": {err.Message}");
                return;
            }

            (var committee, err) = GetCommittee(settings.KeysAddrsFilename);
            if (err != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                await Console.Out.WriteLineAsync($"Invalid key file \"{settings.KeysAddrsFilename}\": {err.Message}");
                return;
            }

            var consensusConfig = Config.New(member, committee);
            DelegateExtensions.InvokeAndCaptureException(Checks.Valid, consensusConfig, out err);
            if (err != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                await Console.Out.WriteLineAsync($"Invalid consensus configuration: {err.Message}");
                return;
            }

            if (settings.Epochs > 0)
            {
                consensusConfig.NumberOfEpochs = settings.Epochs;
            }

            if (settings.Units > 0)
            {
                consensusConfig.EpochLength = settings.Units;
                consensusConfig.LastLevel = consensusConfig.EpochLength + consensusConfig.OrderStartLevel - 1;
            }

            if (settings.Local)
            {
                consensusConfig.IsLocal = true;
            }

            IDataSource ds = new AuremCore.Tests.RandomDataSource(Math.Max(settings.RandomBytesPerUnit, 32));
            if (settings.UseBlockScheduler)
            {
                ds = new AuremCore.Tests.BlockSchedulerDataSource(Math.Max(settings.RandomBytesPerUnit, 32), settings.BlockSchedule, consensusConfig.NProc, consensusConfig.Pid);
            }

            var preblockSink = Channel.CreateUnbounded<Preblock>();
            CancellationTokenSource done = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                try
                {
                    if (settings.Output == 1)
                    {
                        await Console.Out.WriteLineAsync($"PID={consensusConfig.Pid}" + await AuremCore.Tests.PreblockConsumers.ControlSumPreblockConsumer(preblockSink.Reader));
                    }
                    else if (settings.Output == 2)
                    {
                        await AuremCore.Tests.PreblockConsumers.CountingPreblockConsumer(preblockSink.Reader);
                    }
                    else if (settings.Output == 3)
                    {
                        await AuremCore.Tests.PreblockConsumers.PrintingPreblockConsumer(consensusConfig.Pid, preblockSink.Reader);
                    }
                    else
                    {
                        await AuremCore.Tests.PreblockConsumers.NopPreblockConsumer(preblockSink.Reader);
                    }
                }
                finally
                {
                    done.Cancel();
                }
            });

            if (settings.WaitForNodes)
            {
                ConcurrentDictionary<IPAddress, object> acknowledged = new ConcurrentDictionary<IPAddress, object>(new IPAddressEqualityComparer());
                ConcurrentDictionary<IPAddress, object> sentAcknowledge = new ConcurrentDictionary<IPAddress, object>(new IPAddressEqualityComparer());
                List<TcpClient> clients = new List<TcpClient>();
                CancellationTokenSource _ctsWFN = new CancellationTokenSource();
                // self-add
                acknowledged[IPEndPoint.Parse(consensusConfig.FetchAddresses[consensusConfig.Pid]).Address] = new();
                sentAcknowledge[IPEndPoint.Parse(consensusConfig.FetchAddresses[consensusConfig.Pid]).Address] = new();

                async Task ListenForAcknowledge()
                {
                    var listener = new TcpListener(IPAddress.Any, 8367);
                    listener.Start();
                    while (acknowledged.Count < consensusConfig!.NProc)
                    {
                        var tclient = await listener.AcceptTcpClientAsync(_ctsWFN.Token);
                        acknowledged[(tclient.Client.RemoteEndPoint as IPEndPoint)!.Address] = new();
                        clients.Add(tclient);
                        await Console.Out.WriteLineAsync($"WaitForNodes: listened to {(tclient.Client.RemoteEndPoint as IPEndPoint)}");
                    }
                }

                async Task WaitForAll()
                {
                    int i = 0;
                    while (acknowledged.Count < consensusConfig!.NProc || sentAcknowledge.Count < consensusConfig!.NProc)
                    {
                        await Task.Delay(100);
                        i++;
                        if (i % 100 == 0)
                        {
                            await Console.Out.WriteLineAsync("WaitForNodes: still waiting for nodes...");
                        }
                    }

                    await Task.Delay(1000);
                    _ctsWFN.Cancel();
                }

                async Task SendAcknowledge(int i)
                {
                    if (i == consensusConfig.Pid) { return; }
                    var tc = new TcpClient();
                    var rep = IPEndPoint.Parse(consensusConfig!.RMCAddresses[i]);
                    var ep = new IPEndPoint(rep.Address, 8367);
                    
                    while (!tc.Connected)
                    {
                        try
                        {
                            await tc.ConnectAsync(ep);
                            await Console.Out.WriteLineAsync($"WaitForNodes: connected with {ep}");
                        }
                        catch (Exception ex)
                        {
                            await Task.Delay(100);
                            //await Console.Out.WriteLineAsync($"WaitForNodes: failed to connect to node {ep}: {ex.Message}");
                        }
                    }

                    //while (!_ctsWFN.IsCancellationRequested)
                    //{
                    //    try
                    //    {
                    //        await tc.ConnectAsync(ep);
                    //    }
                    //    catch
                    //    {
                    //        await Task.Delay(100);
                    //    }
                    //}

                    sentAcknowledge[ep.Address] = new();
                    clients.Add(tc);
                }

                List<Task> tasks = new List<Task>();
                _ = Task.Factory.StartNew(ListenForAcknowledge);
                for (int i = 0; i < consensusConfig!.NProc; i++)
                {
                    if (i == consensusConfig.Pid) continue;
                    int _i = i;
                    tasks.Add(SendAcknowledge(_i));
                }
                tasks.Add(WaitForAll());

                await Task.WhenAll(tasks);

                foreach (var client in clients)
                {
                    try
                    {
                        client.Dispose();
                    }
                    finally // ignore errors due to disposing on both ends
                    {
                        // ...
                    }
                }

                _ctsWFN.Dispose();

                await Console.Out.WriteLineAsync("WaitForNodes: all nodes online");
            }

            if (settings.Sessions <= 1)
            {
                Func<Task>? start;
                Func<Task>? stop;

                if (settings.Setup)
                {
                    var setupConfig = Config.NewSetup(member, committee);
                    if (settings.Local)
                    {
                        setupConfig.IsLocal = true;
                    }
                    DelegateExtensions.InvokeAndCaptureException(Checks.ValidSetup, setupConfig, out err);
                    if (err != null)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        await Console.Out.WriteLineAsync($"Invalid setup configuration: {err.Message}\n{err.StackTrace}");
                        return;
                    }
                    (start, stop, err) = Aurem.Run.Process.Create(setupConfig, consensusConfig, ds, preblockSink.Writer);
                }
                else
                {
                    (start, stop, err) = Aurem.Run.Process.NoBeacon(consensusConfig, ds, preblockSink.Writer);
                }

                if (err != null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    await Console.Out.WriteLineAsync($"Process failed to start: {err.Message}");
                    return;
                }

                await Console.Out.WriteLineAsync("Starting...");
                await start!.Invoke();
                while (!done.IsCancellationRequested)
                {
                    //Console.WriteLine("Process finished; waiting for termination...");
                    await Task.Delay(3000, done.Token).ContinueWith(t => t.Exception == default);
                }
                await stop!.Invoke();

                await Task.Delay(1000);
            }
            else
            {
                var signaler = Channel.CreateBounded<bool>(1);
                var setupConfig = Config.NewSetup(member, committee);
                DelegateExtensions.InvokeAndCaptureException(Checks.ValidSetup, setupConfig, out err);
                if (err != null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    await Console.Out.WriteLineAsync($"Invalid setup configuration: {err.Message}\n{err.StackTrace}");
                    return;
                }
                (var iterate, err) = Aurem.Run.Process.CreateSessioned(setupConfig, consensusConfig, ds, preblockSink.Writer, signaler.Writer);

                if (err != null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    await Console.Out.WriteLineAsync($"Process failed to start: {err.Message}");
                    return;
                }

                await Console.Out.WriteLineAsync("Starting...");
                for (int i = 0; i < settings.Sessions; i++)
                {
                    await Console.Out.WriteLineAsync($"Starting session {i}...");
                    (var start, var stop, err) = iterate!.Invoke();
                    if (err != null)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        await Console.Out.WriteLineAsync($"Process failed to start session {i}: {err.Message}");
                        return;
                    }
                    await start!.Invoke();

                    // wait for the end of the session
                    await signaler.Reader.ReadAsync();

                    await stop!.Invoke(i == settings.Sessions - 1);
                }
            }
        }
    }
}
