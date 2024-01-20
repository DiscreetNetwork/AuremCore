using Aurem.Config;
using Aurem.Logging;
using Aurem.Model;
using AuremCore.Core;
using AuremCore.FastLogger;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Aurem.Linear
{
    /// <summary>
    /// A component working on a DAG that extends a partial order of units defined by DAG to a linear order.
    /// ExtenderService should be notified, by means of the Notify() method, when it should try to perform its task.
    /// If successful, ExtenderService collects all the units beloning to the newest timing round, and sends them to the output queue.
    /// </summary>
    public class ExtenderService
    {
        public Extender Ordering;
        public ushort Pid;
        public Channel<List<IUnit>> Output;
        public Channel<TimingRound> TimingRounds;
        public long Trigger = 0;
        public long Finished = 0;
        public WaitGroup Wg;
        public Logger Log;
        
        public ExtenderService(IDag dag, IRandomSource rs, Config.Config conf, Channel<List<IUnit>> output, Logger log)
        {
            Log = log.With().Val(Constants.Service, Constants.ExtenderService).Logger();
            Ordering = new Extender(dag, rs, conf, log);
            Pid = conf.Pid;
            Output = output;
            Trigger = 0;
            Finished = 0;
            TimingRounds = Channel.CreateBounded<TimingRound>(Math.Max(conf.EpochLength, 10));
            Wg = new WaitGroup();

            Wg.Add(2);
            _ = Task.Run(async () => await TimingUnitDecider());
            _ = Task.Run(async () => await RoundSorter());
            Log.Info().Msg(Constants.ServiceStarted);
        }

        /// <summary>
        /// Stops the extender.
        /// </summary>
        public async Task Close()
        {
            Interlocked.Exchange(ref Finished, 1);
            await Wg.WaitAsync();
            Log.Info().Msg(Constants.ServiceStopped);
        }

        /// <summary>
        /// Notifies the service to attempt choosing next timing units.
        /// </summary>
        public void Notify()
        {
            Interlocked.Increment(ref Trigger);
        }

        /// <summary>
        /// Tries to pick the next timing unit after receiving notification on trigger (>0). For each picked timing unit, it sends a <see cref="TimingRound"/> to TimingRounds.
        /// </summary>
        /// <returns></returns>
        private async Task TimingUnitDecider()
        {
            try
            {
                while (true)
                {
                    if (Interlocked.Read(ref Finished) > 0)
                    {
                        return;
                    }
                    else if (Interlocked.Read(ref Trigger) > 0)
                    {
                        Interlocked.Decrement(ref Trigger);
                        var round = Ordering.NextRound();
                        while (round != null)
                        {
                            await TimingRounds.Writer.WriteAsync(round);
                            round = Ordering.NextRound();
                        }
                    }
                    

                    await Task.Delay(10);
                }
            }
            finally
            {
                TimingRounds.Writer.Complete();
                Wg.Done();
            }
        }

        /// <summary>
        /// Picks information about newly picked timing units from the TimingRounds queue, finds all units belonging to their timing round and establishes linear order on them.
        /// Sends slices of ordered units to output.
        /// </summary>
        /// <returns></returns>
        private async Task RoundSorter()
        {
            try
            {
                await foreach(var round in TimingRounds.Reader.ReadAllAsync())
                {
                    var units = round.OrderedUnits();
                    Log.Debug().Msg($"units have been received for the timing round");
                    await Output.Writer.WriteAsync(units.ToList());

                    foreach (var u in units)
                    {
                        Log.Debug().Val(Constants.Creator, u.Creator()).Val(Constants.Height, u.Height()).Val(Constants.Epoch, u.EpochID()).Msg(Constants.UnitOrdered);
                        if (u.Creator() == Pid)
                        {
                            Log.Info().Val(Constants.Height, u.Height()).Val(Constants.Level, u.Level()).Msg(Constants.OwnUnitOrdered);
                        }
                    }

                    Log.Info().Val(Constants.Size, units.Count).Msg(Constants.LinearOrderExtended);
                }
            }
            finally
            {
                Wg.Done();
            }
        }
    }
}
