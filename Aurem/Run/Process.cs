using Aurem.Forking;
using Aurem.Logging;
using Aurem.Model;
using Aurem.Ordering;
using Aurem.Random;
using Aurem.Syncing;
using Aurem.Syncing.Internals;
using AuremCore.Core;
using AuremCore.Crypto.Threshold;
using AuremCore.FastLogger;
using AuremCore.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Aurem.Run
{
    public static class Process
    {
        private static void LogWTK(Logger log, WeakThresholdKey wtkey)
        {
            var providers = new List<ushort>(wtkey.ShareProviders.Count);
            foreach (var provider in wtkey.ShareProviders.Keys)
            {
                providers.Add(provider);
            }

            log.Log().Val(Logging.Constants.WTKThreshold, wtkey.Threshold).Vals(Logging.Constants.WTKShareProviders, providers).Msg(Logging.Constants.GotWTK);
        }

        private static (Func<Task>?, Func<Task>?, Exception?) Setup(Config.Config conf, Channel<WeakThresholdKey> wtkchan)
        {
            try
            {
                var log = LoggingUtil.NewLogger(conf);
                var rsf = new Beacon(conf);

                async Task extractHead(IList<IUnit> units)
                {
                    var head = units[^1];
                    if (head.Level() == conf.OrderStartLevel)
                    {
                        await wtkchan.Writer.WriteAsync(rsf.GetWTK(head.Creator()));
                        log.Info().Msg("Setup phase has completed successfully");
                        return;
                    }

                    throw new Exception("Setup phase: wrong level");
                }

                var ord = new Orderer(conf, null!, extractHead, log);
                (var sync, var err) = Syncer.New(conf, ord, log, true);
                if (err != null) throw err;

                var start = () => ord.Start(rsf, sync, NopAlerter.Instance);
                var stop = () => ord.Stop().ContinueWith(x =>
                {
                    log.Stop();
                    wtkchan.Writer.Complete();
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

                return (start, stop, null);
            }
            catch (Exception e)
            {
                return (null, null, e);
            }
        }

        public static (Func<(Func<Task>? Start, Func<Task>? Stop, Exception?)>, Exception?) CreateSessioned(Config.Config setupConf, Config.Config consensusConf, IDataSource ds, ChannelWriter<Preblock> ps, ChannelWriter<bool> ss)
        {
            var netlog = LoggingUtil.NewLogger(consensusConf, 0, true);

            Network setupFetch = new Network(setupConf.FetchAddresses[setupConf.Pid], setupConf.FetchAddresses.ToArray(), netlog, setupConf.Timeout, setupConf, "f");
            Network setupGossip = new Network(setupConf.GossipAddresses[setupConf.Pid], setupConf.GossipAddresses.ToArray(), netlog, setupConf.Timeout, setupConf, "g");
            Network setupMcast = new Network(setupConf.RMCAddresses[setupConf.Pid], setupConf.RMCAddresses.ToArray(), netlog, setupConf.Timeout, setupConf, "r");

            Network fetch = new Network(consensusConf.FetchAddresses[consensusConf.Pid], consensusConf.FetchAddresses.ToArray(), netlog, consensusConf.Timeout, consensusConf, "f");
            Network gossip = new Network(consensusConf.GossipAddresses[consensusConf.Pid], consensusConf.GossipAddresses.ToArray(), netlog, consensusConf.Timeout, consensusConf, "g");
            Network mcast = new Network(consensusConf.MCastAddresses[consensusConf.Pid], consensusConf.MCastAddresses.ToArray(), netlog, consensusConf.Timeout, consensusConf, "m");
            Network netserv = new Network(consensusConf.RMCAddresses[consensusConf.Pid], consensusConf.RMCAddresses.ToArray(), netlog, consensusConf.Timeout, consensusConf, "r");

            // enable sessioned
            setupConf.Sessioned = true;
            consensusConf.Sessioned = true;

            (Func<Task>? Start, Func<Task>? Stop, Exception? Err) MakeSetup(Config.Config c, int session, CancellationTokenSource? t, Channel<WeakThresholdKey> wtk)
            {
                try
                {
                    var conf = c.Clone();
                    conf.Session = session;

                    var log = LoggingUtil.NewLogger(conf, conf.Session);
                    var rsf = new Beacon(conf);

                    async Task extractHead(IList<IUnit> units)
                    {
                        var head = units[^1];
                        if (head.Level() == conf.OrderStartLevel)
                        {
                            long i = 0;
                            if (t != null)
                            {
                                while (!t.IsCancellationRequested)
                                {
                                    await Task.Delay(100);
                                    i++;
                                    if (i % 50 == 0)
                                    {
                                        log.Info().Msg("waiting for signal to proceed...");
                                    }
                                }
                            }

                            await wtk.Writer.WriteAsync(rsf.GetWTK(head.Creator()));
                            log.Info().Msg("Setup phase has completed successfully");
                            return;
                        }

                        throw new Exception("Setup phase: wrong level");
                    }

                    var ord = new Orderer(conf, null!, extractHead, log);
                    (var sync, var err) = Syncer.NewSessioned(conf, ord, log, setupFetch, setupGossip, setupMcast, true);
                    if (err != null) throw err;

                    var start = async () =>
                    {
                        await Console.Out.WriteLineAsync($"started setup for session {session}");
                        await ord.Start(rsf, sync, NopAlerter.Instance);
                    };
                    var stop = () => ord.Stop().ContinueWith(x =>
                    {
                        log.Stop();
                        wtk.Writer.Complete();
                    }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

                    // update networks
                    setupFetch.Update(conf);
                    setupGossip.Update(conf);
                    setupMcast.Update(conf);

                    return (start, stop, null);
                }
                catch (Exception e)
                {
                    return (null, null, e);
                }
            }

            (Func<Task>? Start, Func<Task>? Stop, Exception? Err) MakeConsensus(Config.Config c, int sess, CancellationTokenSource t, Channel<WeakThresholdKey> wtk)
            {
                try
                {
                    var conf = c.Clone();
                    conf.Session = sess;
                    var log = LoggingUtil.NewLogger(conf, conf.Session);

                    async Task makePreblock(IList<IUnit> units)
                    {
                        await ps.WriteAsync(ModelUtils.ToPreblock(units));
                        var timingUnit = units[^1];
                        if (timingUnit.Level() == conf.LastLevel && timingUnit.EpochID() == conf.NumberOfEpochs - 1)
                        {
                            // we no longer need to close the channel
                            await ss.WriteAsync(true);
                            //ps.Complete();
                        }
                    }

                    var ord = new Orderer(conf, ds, makePreblock, log);
                    (var sync, var err) = Syncer.NewSessioned(conf, ord, log, fetch, gossip, mcast, false);
                    if (err != null) throw err;

                    var alert = new AlertService(conf, ord, netserv, log);

                    var started = new TaskCompletionSource();
                    var start = () =>
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                if (wtk.Reader.Completion.IsCompleted) return;
                                var wtkey = await wtk.Reader.ReadAsync();
                                if (wtkey == null) return;

                                LogWTK(log, wtkey);

                                conf.WTKey = wtkey;
                                await ord.Start(new CoinFactory(conf.Pid, wtkey), sync, alert);
                            }
                            finally
                            {
                                started.SetResult();
                            }
                        });

                        return Task.CompletedTask;
                    };
                    var stop = async () =>
                    {
                        await started.Task;
                        await netserv.SoftStopAsync();
                        await ord.Stop();
                        log.Stop();
                        t.Cancel();
                    };

                    gossip.Update(conf);
                    fetch.Update(conf);
                    mcast.Update(conf);
                    netserv.Update(conf);

                    return (start, stop, null);
                }
                catch (Exception e)
                {
                    return (null, null, e);
                }
            }

            int session = 0;
            var prevWTK = Channel.CreateBounded<WeakThresholdKey>(1);
            var prevSetup = MakeSetup(setupConf, session, null!, prevWTK);

            Func<(Func<Task>?, Func<Task>?, Exception?)> iterate = () =>
            {
                var wtkchan = Channel.CreateBounded<WeakThresholdKey>(1);
                var cts = new CancellationTokenSource();
                var _prevSetup = prevSetup;
                var _prevWTK = prevWTK;
                (var startSetup, var stopSetup, var setupErr) = MakeSetup(setupConf, session + 1, cts, wtkchan);
                if (setupErr != null)
                {
                    return (null, null, setupErr);
                }

                (var startConsensus, var stopConsensus, var consensusErr) = MakeConsensus(consensusConf, session, cts, _prevWTK);
                if (consensusErr != null)
                {
                    return (null, null, consensusErr);
                }

                Func<Task>? start;
                Func<Task>? stop;

                if (session == 0)
                {
                    if (_prevSetup.Err != null)
                    {
                        return (null, null, prevSetup.Err);
                    }
                }

                var startSess = session;
                start = async () =>
                {
                    if (startSess == 0)
                    {
                        await Console.Out.WriteLineAsync($"starting prevSetup");
                        await _prevSetup.Start!();
                    }
                    await Task.WhenAll(startSetup!(), startConsensus!());
                };

                var stopSess = session;
                stop = async () =>
                {
                    if (stopSess == 0) await _prevSetup.Stop!();
                    // these need to be cancelled in parallel
                    await Task.WhenAll(stopSetup!(), stopConsensus!());
                };

                prevSetup = (startSetup, stopSetup, setupErr);
                prevWTK = wtkchan;
                session++;

                return (start, stop, null);
            };

            return (iterate, null);
        }

        private static (Func<Task>?, Func<Task>?, Exception?) Consensus(Config.Config conf, Channel<WeakThresholdKey> wtkchan, IDataSource ds, ChannelWriter<Preblock> sink)
        {
            try
            {
                var log = LoggingUtil.NewLogger(conf);

                async Task makePreblock(IList<IUnit> units)
                {
                    await sink.WriteAsync(ModelUtils.ToPreblock(units));
                    var timingUnit = units[^1];
                    if (timingUnit.Level() == conf.LastLevel && timingUnit.EpochID() == conf.NumberOfEpochs - 1)
                    {
                        sink.Complete();
                    }
                }

                var ord = new Orderer(conf, ds, makePreblock, log);
                (var sync, var err) = Syncer.New(conf, ord, log, false);
                if (err != null) throw err;

                var netserv = new Network(conf.RMCAddresses[conf.Pid], conf.RMCAddresses.ToArray(), log, conf.Timeout, conf);
                var alert = new AlertService(conf, ord, netserv, log);

                var started = new TaskCompletionSource();
                var start = () =>
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            if (wtkchan.Reader.Completion.IsCompleted) return;
                            var wtkey = await wtkchan.Reader.ReadAsync();
                            if (wtkey == null) return;

                            LogWTK(log, wtkey);

                            conf.WTKey = wtkey;
                            await ord.Start(new CoinFactory(conf.Pid, wtkey), sync, alert);
                        }
                        finally
                        {
                            started.SetResult();
                        }
                    });

                    return Task.CompletedTask;
                };
                var stop = async () =>
                {
                    await started.Task;
                    await netserv.StopAsync();
                    await ord.Stop();
                    log.Stop();
                };

                return (start, stop, null);
            }
            catch (Exception e)
            {
                return (null, null, e);
            }
        }

        public static (Func<Task>?, Func<Task>?, Exception?) NoBeacon(Config.Config conf, IDataSource ds, ChannelWriter<Preblock> ps)
        {
            var wtkchan = Channel.CreateBounded<WeakThresholdKey>(1);
            wtkchan.Writer.TryWrite(WeakThresholdKey.Seeded(conf.NProc, conf.Pid, 2137, null));
            (var start, var stop, var err) = Consensus(conf, wtkchan, ds, ps);

            return (start, stop, err);
        }
        /// <summary>
        /// Main external API of the Aurem testing framework. Takes two configs, one for setup and one for consensus, as well as a data source and a preblock sink.
        /// Initializes two orderers and a channel connecting them used for passing the result of the setup phase.
        /// Returns two functions which can be used to start and stop the system, which are null when an exception occurs when setting up the two functions.
        /// The provided channelwriter as the preblock sink is signaled to close when the last preblock is produced.
        /// </summary>
        /// <param name="setupConf"></param>
        /// <param name="conf"></param>
        /// <param name="ds"></param>
        /// <param name="ps"></param>
        /// <returns></returns>
        public static (Func<Task>? Start, Func<Task>? Stop, Exception? Ex) Create(Config.Config setupConf, Config.Config conf, IDataSource ds, ChannelWriter<Preblock> ps)
        {
            var wtkchan = Channel.CreateBounded<WeakThresholdKey>(1);
            (var startSetup, var stopSetup, var setupErr) = Setup(setupConf, wtkchan);
            if (setupErr != null)
            {
                return (null, null, new Exception($"an error occurred while initializing setup: {setupErr.Message}"));
            }

            (var startConsensus, var stopConsensus, var consensusErr) = Consensus(conf, wtkchan, ds, ps);
            if (consensusErr != null)
            {
                return (null, null, new Exception($"an error occurred while initializing consensus: {consensusErr.Message}"));
            }

            var start = async () =>
            {
                await startSetup!();
                await startConsensus!();
            };
            var stop = async () =>
            {
                await stopSetup!();
                await stopConsensus!();
            };

            return (start, stop, null);
        }
    }
}
