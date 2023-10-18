using Aurem.Config;
using AuremCore.Core;
using AuremCore.Core.Extensions;
using AuremCore.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
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

            var ds = (settings.RandomBytesPerUnit >= 32) ? new AuremCore.Tests.RandomDataSource(settings.RandomBytesPerUnit) : new AuremCore.Tests.RandomDataSource(32);

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
