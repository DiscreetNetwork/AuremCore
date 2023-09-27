using AuremCore.Core;
using AuremTests.Cmd;
using System;

namespace AuremCore
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var nproc = 4;
            var conf = new RunAurem.AuremSettings[nproc];
            for (int i = 0; i < nproc; i++)
            {
                conf[i] = new RunAurem.AuremSettings();

                conf[i].RandomBytesPerUnit = 1024;
                conf[i].KeysAddrsFilename = "committee.ka";
                conf[i].PrivFilename = $"{i}.pk";
            }
            

            await Console.Out.WriteLineAsync("Generating keys...");
            KeyGen.Generate(nproc);

            await Console.Out.WriteLineAsync("Generated keys, waiting for 5 seconds...");
            await Task.Delay(5000);

            var wg = new WaitGroup();
            wg.Add(nproc);
            for (int i = 0; i < nproc; i++)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await RunAurem.Run(conf[i]);
                    }
                    catch (Exception e)
                    {
                        await Console.Out.WriteLineAsync($"FATAL; Process {i} failed to complete: {e.Message}");
                    }
                    finally
                    {
                        wg.Done();
                    }
                });
            }
        }
    }
}