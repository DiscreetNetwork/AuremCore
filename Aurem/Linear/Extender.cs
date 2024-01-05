using Aurem.Config;
using Aurem.Logging;
using Aurem.Model;
using AuremCore.FastLogger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Linear
{
    /// <summary>
    /// A class that implements an algorithm that extends the order of units provided by an instance of a DAG to a linear order.
    /// </summary>
    public class Extender
    {
        public Dictionary<Hash, SuperMajorityDecider> Deciders;
        public IDag Dag;
        public IRandomSource RandomSource;
        public List<IUnit> LastTUs;
        public IUnit CurrentTU = null;
        public bool LastDecideResult;
        public int ZeroVoteRoundForCommonVote;
        public int FirstDecidingRound;
        public int OrderStartLevel;
        public int CommonVoteDeterministicPrefix;
        public CommonRandomPermutation CRPIterator;
        public Logger Log;

        private ushort Pid;

        public Extender(IDag dag, IRandomSource rs, Config.Config conf, Logger log)
        {
            Dag = dag;
            RandomSource = rs;
            Deciders = new(new Hash.HashEqualityComparer());
            LastTUs = new List<IUnit>(Enumerable.Repeat<IUnit>(null, conf.ZeroVoteRoundForCommonVote));
            ZeroVoteRoundForCommonVote = conf.ZeroVoteRoundForCommonVote;
            FirstDecidingRound = conf.FirstDecidingRound;
            OrderStartLevel = conf.OrderStartLevel;
            CommonVoteDeterministicPrefix = conf.CommonVoteDeterministicPrefix;
            CRPIterator = new CommonRandomPermutation(dag, rs, conf.CRPFixedPrefix);
            Log = log;
            Pid = conf.Pid;
        }

        public static int DagMaxLevel(IDag dag)
        {
            var maxLevel = -1;
            dag.MaximalUnitsPerProcess().Iterate(units =>
            {
                foreach (var v in units)
                {
                    if (v.Level() > maxLevel) maxLevel = v.Level();
                }
                return true;
            });

            return maxLevel;
        }

        public SuperMajorityDecider GetDecider(IUnit uc)
        {
            var success = Deciders.TryGetValue(uc.Hash(), out var decider);
            if (!success || decider == null)
            {
                decider = new SuperMajorityDecider(uc, Dag, RandomSource, CommonVoteDeterministicPrefix, ZeroVoteRoundForCommonVote);
                Deciders[uc.Hash()] = decider;
            }

            return decider;
        }

        public TimingRound? NextRound()
        {
            if (LastDecideResult) LastDecideResult = false;

            var dagMaxLevel = DagMaxLevel(Dag);
            if (dagMaxLevel < OrderStartLevel) return null;

            var level = OrderStartLevel;
            if (CurrentTU != null) level = CurrentTU.Level() + 1;
            if (dagMaxLevel < level + FirstDecidingRound) return null;

            var previousTU = CurrentTU;
            var decided = false;
            var randomBytesPresent = CRPIterator.CRPIterate(level, previousTU, uc =>
            {
                var decider = GetDecider(uc);
                (var decision, var decidedOn) = decider.DecideUnitIsPopular(dagMaxLevel);
                
                if (decision == Vote.Popular)
                {
                    Log.Info().Val(Constants.Height, decidedOn).Val(Constants.Size, dagMaxLevel).Val(Constants.Round, level).Msg(Constants.NewTimingUnit);
                    LastTUs = LastTUs.GetRange(1, LastTUs.Count - 1);
                    LastTUs.Add(CurrentTU);
                    CurrentTU = uc;
                    LastDecideResult = true;
                    Deciders = new Dictionary<Hash, SuperMajorityDecider>();

                    decided = true;
                    return false;
                }

                if (decision == Vote.Undecided)
                {
                    return false;
                }

                return true;
            });

            if (!randomBytesPresent) Log.Debug().Val(Constants.Round, level).Msg(Constants.MissingRandomBytes);

            if (!decided) return null;
            var tr = new TimingRound(CurrentTU, LastTUs);
            return tr;
        }
    }
}
