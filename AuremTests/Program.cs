﻿using Aurem.Config;
using AuremCore.Core;
using AuremCore.Crypto.Threshold;
using AuremCore.Tests;
using AuremTests.Cmd;
using AuremTests.Packets;
using BN256Core;
using BN256Core.Extensions;
using System;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace AuremCore
{
    public class Program
    {
        public static readonly bool UseLocalServers = false;

        public static async Task RunLocalLegacy()
        {
            var nproc = 4;
            for (int i = 0; i < nproc; i++)
            {
                if (File.Exists($"{i}.setup.hr.txt")) File.Delete($"{i}.setup.hr.txt");
                if (File.Exists($"{i}.hr.txt")) File.Delete($"{i}.hr.txt");
            }

            await Task.Delay(500);

            for (int i = 0; i < nproc; i++)
            {
                if (File.Exists($"{i}.setup.json")) HRLog.MakeReadableLog($"{i}.setup.json", $"{i}.setup.hr.txt");
                if (File.Exists($"{i}.json")) HRLog.MakeReadableLog($"{i}.json", $"{i}.hr.txt");
            }

            // create polyverifier
            //var pv = PolyVerifier.New(4, TUtil.MinimalTrusted(4));
            //pv.


            var conf = new RunAurem.AuremSettings[nproc];
            for (int i = 0; i < nproc; i++)
            {
                conf[i] = new RunAurem.AuremSettings();

                conf[i].RandomBytesPerUnit = 1048576;
                conf[i].KeysAddrsFilename = "committee.ka";
                conf[i].PrivFilename = $"{i}.pk";
                conf[i].UseLocalServer = UseLocalServers;
                conf[i].Output = 3;
                conf[i].UseBlockScheduler = true;
                //conf[i].Setup = false;
            }


            await Console.Out.WriteLineAsync("Generating keys...");
            KeyGen.Generate(nproc);

            await Console.Out.WriteLineAsync("Generated keys, waiting for 3 seconds...");
            await Task.Delay(3000);


            if (UseLocalServers)
            {
                await Console.Out.WriteLineAsync("creating virtual servers...");
                //LocalServer.Instances = LocalServer.NewNetwork(nproc, TimeSpan.FromSeconds(5));
                await Console.Out.WriteLineAsync("virtual servers created.");
            }

            var wg = new WaitGroup();
            wg.Add(nproc);
            for (int i = 0; i < nproc; i++)
            {
                var _i = i;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await RunAurem.Run(conf[_i]);
                    }
                    catch (Exception e)
                    {
                        await Console.Out.WriteLineAsync($"FATAL; Process {_i} failed to complete: {e.Message}\n{e.StackTrace}");
                    }
                    finally
                    {
                        await Console.Out.WriteLineAsync($"Node {_i} has completed.");
                        wg.Done();
                    }
                });
            }

            await wg.WaitAsync();
        }

        public static async Task BuildCommittee(string registryEndpoint, string memberPath, string committeePath)
        {
            var ports = new ushort[] { 8360, 8361, 8362, 8363, 8364, 8365, 8366, 8367 };
            bool success = IPEndPoint.TryParse(registryEndpoint, out var endpoint);

            if (!success)
            {
                await Console.Out.WriteLineAsync("BuildCommittee: could not parse registry endpoint");
                return;
            }

            // build ports packet and connection
            var rp = new RegisterPacket(ports);
            var client = new TcpClient();
            try
            {
                var estabCts = new CancellationTokenSource();
                estabCts.CancelAfter(TimeSpan.FromSeconds(5));
                await client.ConnectAsync(endpoint!, estabCts.Token);
                if (estabCts.IsCancellationRequested)
                {
                    throw new Exception("connecting to registry timed out");
                }

                estabCts.Dispose();
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync($"BuildCommittee: failed to connect to registry server: {ex.Message}");
                return;
            }

            // attempt registration
            try
            {
                var rpCts = new CancellationTokenSource();
                rpCts.CancelAfter(TimeSpan.FromSeconds(10));
                await client.GetStream().WriteAsync(rp.Serialize(), rpCts.Token);

                if (rpCts.IsCancellationRequested)
                {
                    throw new Exception("write RegisterPacket timed out");
                }

                rpCts.Dispose();

                var ack = new byte[1];
                await client.GetStream().ReadAsync(ack);

                if (ack[0] == 0xff)
                {
                    // register failed
                    var szb = new byte[4];
                    await client.GetStream().ReadAsync(szb);
                    var sz = BinaryPrimitives.ReadUInt32LittleEndian(szb);
                    var errb = new byte[sz];
                    await client.GetStream().ReadAsync(errb);
                    var errp = new ErrorsPacket(errb);

                    throw new Exception($"the following ports of ours were not open: {errp.Errors.Aggregate("", (x, y) => x == "" ? $"{y}" : $"{x}, {y}")}");
                }
                else if (ack[0] == 0x01)
                {
                    // we were validated
                }
                else
                {
                    throw new Exception($"invalid ACK received: {ack[0]}");
                }
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync($"BuildCommittee: failed to add ourself to registry server: {ex.Message}");
                return;
            }

            // now wait to receive registry
            List<RegisterPacket> registry = new List<RegisterPacket>();
            try
            {
                while (!client.GetStream().DataAvailable)
                {
                    await Task.Delay(100);
                }

                var buf = new byte[4];
                await client.GetStream().ReadAsync(buf);
                var numRegistrants = BinaryPrimitives.ReadUInt32LittleEndian(buf);

                for (int i = 0; i < numRegistrants; i++)
                {
                    var b = new byte[RegisterPacket.RegistrySize];
                    await client.GetStream().ReadAsync(b);
                    registry.Add(new RegisterPacket(b, true));
                }
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync($"BuildCommittee: failed to receive registry: {ex.Message}");
                return;
            }

            if (registry.Count < 4)
            {
                await Console.Out.WriteLineAsync($"BuildCommittee: received less than the minimum required registry entries for consensus");
                return;
            }

            // calculate key material and send data
            List<KeyMaterialPacket> keymats = new List<KeyMaterialPacket>();
            var ourKeyMat = new KeyMaterialPacket();
            try
            {
                var ourKMb = ourKeyMat.Serialize();
                List<Exception> errs = new List<Exception>();

                async Task connectToClient(RegisterPacket p)
                {
                    try
                    {
                        var ncts = new CancellationTokenSource();
                        ncts.CancelAfter(TimeSpan.FromSeconds(5));
                        var nclient = new TcpClient();
                        await nclient.ConnectAsync(p.Address, p.SetupCommitteePort, ncts.Token);

                        if (ncts.IsCancellationRequested)
                        {
                            throw new Exception("connection timed out");
                        }
                        if (!nclient.Connected)
                        {
                            throw new Exception("could not connect");
                        }

                        var scts = new CancellationTokenSource();
                        scts.CancelAfter(TimeSpan.FromSeconds(10));
                        await nclient.GetStream().WriteAsync(ourKMb, scts.Token);

                        if (scts.IsCancellationRequested)
                        {
                            throw new Exception("sending key data timed out");
                        }
                    }
                    catch (Exception ex)
                    {
                        await Console.Out.WriteLineAsync($"BuildCommittee: could not connect to consensus node at {new IPEndPoint(p.Address, p.SetupCommitteePort)}: {ex.Message}");
                        errs.Add(ex);
                        return;
                    }
                }

                async Task listenToConsensus()
                {
                    var listener = new TcpListener(IPAddress.Any, rp!.SetupCommitteePort);
                    var ctsTot = new CancellationTokenSource();
                    ctsTot.CancelAfter(TimeSpan.FromSeconds(30));
                    try
                    {
                        while (keymats.Count < registry.Count && errs.Count == 0 && !ctsTot.IsCancellationRequested)
                        {
                            var listen = await listener.AcceptTcpClientAsync();

                            while (!listen.GetStream().DataAvailable) await Task.Delay(10);

                            // receive data
                            var kmb = new byte[KeyMaterialPacket.Size];
                            var rkmcts = new CancellationTokenSource();
                            rkmcts.CancelAfter(TimeSpan.FromSeconds(10));
                            await listen.GetStream().ReadAsync(kmb, rkmcts.Token);

                            if (rkmcts.IsCancellationRequested)
                            {
                                throw new Exception("reading timed out");
                            }

                            var kmp = new KeyMaterialPacket(kmb);
                            keymats.Add(kmp);

                            rkmcts.Dispose();
                        }

                        if (ctsTot.IsCancellationRequested)
                        {
                            throw new Exception("total listen time exceeded");
                        }
                    }
                    catch (Exception ex)
                    {
                        await Console.Out.WriteLineAsync($"BuildCommittee: failed to retrieve key material for a node: {ex.Message}");
                        errs.Add(ex);
                        return;
                    }
                }
                
                List<Task> broadcastTasks = new List<Task>();
                foreach (var node in registry)
                {
                    broadcastTasks.Add(connectToClient(node));
                }
                broadcastTasks.Add(listenToConsensus());
                await Task.WhenAll(broadcastTasks);

                if (errs.Count > 0 && errs.Any(x => x != null))
                {
                    await Console.Out.WriteLineAsync($"BuildCommittee: one or more nodes did not receive our packet, or receiving a packet from a node failed");
                    return;
                }
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync($"BuildCommittee: key material phase failed: {ex.Message}");
                return;
            }

            // order everything
            try
            {
                List<(RegisterPacket, KeyMaterialPacket)> committeeData = registry.Zip(keymats).ToList();
                committeeData.Sort((x, y) =>
                {
                    var h1 = SHA256.HashData(x.Item2.PublicKey.Serialize());
                    var h2 = SHA256.HashData(x.Item2.PublicKey.Serialize());

                    return h1.Compare(h2);
                });

                var setupAddresses = new Dictionary<string, List<string>>
            {
                { "rmc", new() },
                { "fetch", new() },
                { "gossip", new() }
            };
                var addresses = new Dictionary<string, List<string>>
            {
                { "rmc", new() },
                { "mcast", new() },
                { "fetch", new() },
                { "gossip", new() }
            };

                for (int i = 0; i < committeeData.Count; i++)
                {
                    setupAddresses["rmc"].Add(committeeData[i].Item1.Address.ToString() + $":{committeeData[i].Item1.SetupRmcPort}");
                    setupAddresses["fetch"].Add(committeeData[i].Item1.Address.ToString() + $":{committeeData[i].Item1.SetupFetchPort}");
                    setupAddresses["gossip"].Add(committeeData[i].Item1.Address.ToString() + $":{committeeData[i].Item1.SetupGossipPort}");
                    addresses["rmc"].Add(committeeData[i].Item1.Address.ToString() + $":{committeeData[i].Item1.RmcPort}");
                    addresses["mcast"].Add(committeeData[i].Item1.Address.ToString() + $":{committeeData[i].Item1.McastPort}");
                    addresses["fetch"].Add(committeeData[i].Item1.Address.ToString() + $":{committeeData[i].Item1.FetchPort}");
                    addresses["gossip"].Add(committeeData[i].Item1.Address.ToString() + $":{committeeData[i].Item1.GossipPort}");
                }

                var committee = new Committee
                {
                    SetupAddresses = setupAddresses,
                    Addresses = addresses,
                    PublicKeys = committeeData.Select(x => x.Item2.PublicKey).ToList(),
                    RMCVerificationKeys = committeeData.Select(x => x.Item2.VerKey).ToList(),
                    P2PPublicKeys = committeeData.Select(x => x.Item2.P2PPubKey).ToList(),
                };

                // find our Pid
                ushort pid = 0;
                while (true)
                {
                    if (pid >= committeeData.Count)
                    {
                        throw new Exception("could not find our Pid");
                    }

                    if (committeeData[pid].Item2.VerKey.Marshal().BEquals(ourKeyMat.SecKey!.VerificationKey().Marshal()))
                    {
                        break;
                    }

                    pid++;
                }

                var member = new Member
                {
                    Pid = pid,
                    P2PSecretKey = ourKeyMat.P2PSecKey!,
                    PrivateKey = ourKeyMat.PrivateKey!,
                    RMCSecretKey = ourKeyMat.SecKey!
                };

                if (File.Exists(memberPath)) File.Delete(memberPath);
                await Task.Delay(500);
                var fw = File.Create(memberPath);
                member.StoreMember(fw);

                fw.Flush();
                fw.Close();

                if (File.Exists(committeePath)) File.Delete(committeePath);
                await Task.Delay(500);
                var fc = File.Create(committeePath);
                committee.StoreCommittee(fc);

                fc.Flush();
                fc.Close();

                await Console.Out.WriteLineAsync($"BuildCommittee: completed successfully");
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync($"BuildCommittee: could not build committee and member files: {ex.Message}");
            }
        }

        public static void PrintHelp()
        {
            Console.WriteLine("AuremTests:");
            Console.WriteLine("    usage: ./AuremTests <cmd> ...");
            Console.WriteLine();
            Console.WriteLine("Subcommands:");
            Console.WriteLine("    hr - converts all logs into human readable formats");
            Console.WriteLine("    gen|keygen <nproc> - generates a local committee and <nproc> member files indexed by pid");
            Console.WriteLine("    local - runs a local instance of the aurem protocol from the committee and member files available in the current directory");
            Console.WriteLine("    build <registryEndpoint> <memberFilePath> <committeeFilePath> - communicates with registry and other nodes to build a consensus committee file and member file");
            Console.WriteLine("    run <memberFilePath> <committeeFilePath> - runs the aurem protocol using the specified committee and member files");
        }

        public static async Task Run(string[] args)
        {
            if (args.Length == 0)
            {
                PrintHelp();
                return;
            }

            if (args[0] == "hr")
            {
                var fs = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.hr.txt");
                Console.WriteLine();
                foreach (var f in fs)
                {
                    if (File.Exists(f)) File.Delete(f);
                }

                await Task.Delay(500);

                var ls = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.json");

                foreach (var lf in ls)
                {
                    var of = lf[..^5];
                    HRLog.MakeReadableLog(lf, $"{of}.hr.txt");
                }

                return;
            }
            else if (args[0] == "gen" || args[0] == "keygen")
            {
                await Console.Out.WriteLineAsync("Generating keys...");
                KeyGen.Generate(int.Parse(args[1]));
            }
            else if (args[0] == "local")
            {
                await RunLocalLegacy();
            }
            else if (args[0] == "build")
            {
                if (args.Length < 2)
                {
                    await Console.Out.WriteLineAsync("BuildCommittee: expects at least one keyword (help to see command format)");
                    return;
                }

                if (args[1] == "help")
                {
                    await Console.Out.WriteLineAsync("BuildCommittee: build <registryEndpoint> <memberFilePath> <committeeFilePath>");
                    return;
                }

                if (args.Length < 4)
                {
                    await Console.Out.WriteLineAsync("BuildCommittee: invalid format (help to see command format)");
                    return;
                }

                await BuildCommittee(args[1], args[2], args[3]);
            }
            else if (args[0] == "run")
            {
                if (args.Length < 2)
                {
                    await Console.Out.WriteLineAsync("Run: expects at least one keyword (help to see command format)");
                    return;
                }

                if (args[1] == "help")
                {
                    await Console.Out.WriteLineAsync("Run: run <memberFilePath> <committeeFilePath>");
                    return;
                }

                if (args.Length < 3)
                {
                    await Console.Out.WriteLineAsync("Run: invalid format (help to see command format)");
                    return;
                }

                var conf = new RunAurem.AuremSettings();

                conf.RandomBytesPerUnit = 1048576;
                conf.KeysAddrsFilename = args[2];
                conf.PrivFilename = args[1];
                conf.UseLocalServer = UseLocalServers;
                conf.Output = 3;
                conf.UseBlockScheduler = true;
                conf.WaitForNodes = true;

                try
                {
                    await RunAurem.Run(conf);
                }
                catch (Exception e)
                {
                    await Console.Out.WriteLineAsync($"FATAL; Process failed to complete: {e.Message}\n{e.StackTrace}");
                }
                finally
                {
                    await Console.Out.WriteLineAsync($"Node has completed.");
                }
            }
            else if (args[0] == "forktest")
            {
                await TestForking.TestAlert();
            }
            else
            {
                PrintHelp();
            }
        }

        public static async Task Main(string[] args)
        {
            await Run(new string[] {"forktest"});
        }
    }
}