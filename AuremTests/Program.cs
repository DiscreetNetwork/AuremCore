using AuremCore.Core;
using AuremCore.Crypto.Threshold;
using AuremCore.Tests;
using AuremTests.Cmd;
using BN256Core;
using System;

namespace AuremCore
{
    public class Program
    {
        public static readonly bool UseLocalServers = false;

        public static async Task Main(string[] args)
        {
            if (File.Exists("1.setup.hr.txt")) File.Delete("1.setup.hr.txt");
            if (File.Exists("0.setup.hr.txt")) File.Delete("0.setup.hr.txt");
            if (File.Exists("2.setup.hr.txt")) File.Delete("2.setup.hr.txt");
            if (File.Exists("3.setup.hr.txt")) File.Delete("3.setup.hr.txt");
            if (File.Exists("1.hr.txt")) File.Delete("1.hr.txt");
            if (File.Exists("0.hr.txt")) File.Delete("0.hr.txt");
            if (File.Exists("2.hr.txt")) File.Delete("2.hr.txt");
            if (File.Exists("3.hr.txt")) File.Delete("3.hr.txt");

            await Task.Delay(500);

            HRLog.MakeReadableLog("1.setup.json", "1.setup.hr.txt");
            HRLog.MakeReadableLog("0.setup.json", "0.setup.hr.txt");
            HRLog.MakeReadableLog("2.setup.json", "2.setup.hr.txt");
            HRLog.MakeReadableLog("3.setup.json", "3.setup.hr.txt");
            HRLog.MakeReadableLog("1.json", "1.hr.txt");
            HRLog.MakeReadableLog("0.json", "0.hr.txt");
            HRLog.MakeReadableLog("2.json", "2.hr.txt");
            HRLog.MakeReadableLog("3.json", "3.hr.txt");

            // create polyverifier
            //var pv = PolyVerifier.New(4, TUtil.MinimalTrusted(4));
            //pv.

            var nproc = 4;
            var conf = new RunAurem.AuremSettings[nproc];
            for (int i = 0; i < nproc; i++)
            {
                conf[i] = new RunAurem.AuremSettings();

                conf[i].RandomBytesPerUnit = 1024;
                conf[i].KeysAddrsFilename = "committee.ka";
                conf[i].PrivFilename = $"{i}.pk";
                conf[i].UseLocalServer = UseLocalServers;
                conf[i].Output = 3;
                conf[i].Setup = false;
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
    }
}