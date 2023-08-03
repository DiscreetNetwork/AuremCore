using Aurem.Config;
using Aurem.Logging;
using Aurem.Logging.FastLogger;
using Aurem.Model;
using AuremCore.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public ConcurrentQueue<List<IUnit>> Output;
        public long TimingRoundsRemaining = 0;
        public ConcurrentQueue<TimingRound> TimingRounds;
        public long Trigger = 0;
        public long Finished = 0;
        public Logger Log;
        
        public ExtenderService(IDag dag, IRandomSource rs, Config.Config conf, ConcurrentQueue<List<IUnit>> output, Logger log)
        {
            Log = log.With().Val(Constants.Service, Constants.ExtenderService).Logger();
            Ordering = new Extender(dag, rs, conf, log);
            Pid = conf.Pid;
            Output = output;
            Trigger = 0;
            Finished = 0;
            TimingRounds = new();
            TimingRoundsRemaining = conf.EpochLength;

            _ = Task.Run(async () => await TimingUnitDecider());
            _ = Task.Run(async () => await RoundSorter());
            Log.Info().Msg(Constants.ServiceStarted);
        }

        /// <summary>
        /// Stops the extender.
        /// </summary>
        public void Close()
        {
            Interlocked.Exchange(ref Trigger, 0);
            // TODO: implement means of waiting for work to finish.
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
            while (true)
            {
                if (Interlocked.Read(ref Trigger) > 0)
                {
                    Interlocked.Decrement(ref Trigger);
                    var round = Ordering.NextRound();
                    while (round != null)
                    {
                        TimingRounds.Enqueue(round);
                        round = Ordering.NextRound();
                    }
                }
                else if (Interlocked.Read(ref Finished) > 0)
                {
                    Interlocked.Exchange(ref Finished, 0);
                    return;
                }

                await Task.Delay(10);
            }
        }

        /// <summary>
        /// Picks information about newly picked timing units from the TimingRounds queue, finds all units belonging to their timing round and establishes linear order on them.
        /// Sends slices of ordered units to output.
        /// </summary>
        /// <returns></returns>
        private async Task RoundSorter()
        {
            while (Interlocked.Read(ref TimingRoundsRemaining) > 0)
            {
                while (TimingRounds.IsEmpty)
                {
                    await Task.Delay(10);
                }

                TimingRounds.TryDequeue(out var round);

                var units = round.OrderedUnits();
                Output.Enqueue(units.ToList());

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
    }
}
