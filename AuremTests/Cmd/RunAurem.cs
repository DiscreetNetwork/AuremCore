﻿using Aurem.Config;
using AuremCore.Core;
using AuremCore.Core.Extensions;
using AuremCore.Tests;
using System;
using System.Collections.Generic;
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

            IDataSource ds = (settings.RandomBytesPerUnit >= 32) ? new AuremCore.Tests.RandomDataSource(settings.RandomBytesPerUnit) : new AuremCore.Tests.RandomDataSource(32);
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

            Func<Task>? start;
            Func<Task>? stop;

            if (settings.Setup)
            {
                var setupConfig = Config.NewSetup(member, committee);
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

            if (settings.WaitForNodes)
            {
                List<object> acknowledged = new List<object>();
                List<object> sentAcknowledge = new List<object>();
                List<TcpClient> clients = new List<TcpClient>();
                CancellationTokenSource _ctsWFN = new CancellationTokenSource();
                // self-add
                acknowledged.Add(new());
                sentAcknowledge.Add(new());

                async Task ListenForAcknowledge()
                {
                    var listener = new TcpListener(IPAddress.Any, 8367);
                    listener.Start();
                    while (acknowledged.Count < consensusConfig!.NProc)
                    {
                        var tclient = await listener.AcceptTcpClientAsync(_ctsWFN.Token);
                        acknowledged.Add(new object());
                        clients.Add(tclient);
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
                            await Console.Out.WriteLineAsync("WaitForNodes:");
                        }
                        catch (Exception ex)
                        {
                            await Task.Delay(100);
                            //await Console.Out.WriteLineAsync($"WaitForNodes: failed to connect to node {ep}: {ex.Message}");
                        }
                    }

                    sentAcknowledge.Add(new object());
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

            await start!.Invoke();
            while (!done.IsCancellationRequested)
            {
                //Console.WriteLine("Process finished; waiting for termination...");
                await Task.Delay(3000, done.Token).ContinueWith(t => t.Exception == default);
            }
            await stop!.Invoke();

            await Task.Delay(1000);
        }
    }
}
