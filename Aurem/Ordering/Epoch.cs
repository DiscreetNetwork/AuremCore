using Aurem.Adding;
using Aurem.Dag;
using Aurem.Linear;
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

namespace Aurem.Ordering
{
    public class Epoch
    {
        public uint EpochID;
        public IAdder Adder;
        public IDag Dag;
        public ExtenderService Extender;
        public IRandomSource Rs;
        public Logger Log;

        private Channel<bool> More;

        public Epoch(uint id, Config.Config conf, ISyncer syncer, IRandomSourceFactory rsf, IAlerter alert, Channel<IUnit> belt, Channel<List<IUnit>> output, Logger log)
        {
            Log = log.With().Val(Logging.Constants.Epoch, id).Logger();
            Dag = new DAG(conf, id);
            Adder = new Adder(Dag, conf, syncer, alert, Log);
            Rs = rsf.CreateRandomSource(Dag);
            Extender = new ExtenderService(Dag, Rs, conf, output, Log);

            Dag.AfterInsert(_ => Extender.Notify());
            Dag.AfterInsert(u =>
            {
                Log.Debug().Val(Logging.Constants.Creator, u.Creator()).Val(Logging.Constants.Epoch, u.EpochID()).Val(Logging.Constants.Height, u.Height()).Val(Logging.Constants.Level, u.Level()).Msg(Logging.Constants.SendingUnitToCreator);
                if (u.Creator() != conf.Pid)
                {
                    //Log.Debug().Val(Logging.Constants.Creator, u.Creator()).Val(Logging.Constants.Height, u.Height()).Val(Logging.Constants.Level, u.Level()).Msg(Logging.Constants.SendingUnitToCreator);
                    belt.Writer.TryWrite(u);
                }
            });

            Log.Log().Msg(Logging.Constants.NewEpoch);
            More = Channel.CreateBounded<bool>(1);
            EpochID = id;
        }

        public async Task Close()
        {
            await Adder.Close();
            await Extender.Close();
            Log.Log().Msg(Logging.Constants.EpochEnd);
        }

        public List<IUnit> UnitsAbove(IList<int> heights) => Dag.UnitsAbove(heights);

        public List<IUnit> AllUnits() => Dag.UnitsAbove(null);

        public async Task<bool> WantsMoreUnits()
        {
            if (!More.Reader.Completion.IsCompleted)
            {
                return false;
            }

            var success = More.Reader.TryRead(out bool rv);
            if (success)
            {
                return rv;
            }

            return false;
        }

        public void NoMoreUnits() => More.Writer.Complete();
    }
}
