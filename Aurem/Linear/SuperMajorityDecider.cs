using Aurem.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Linear
{
    public class SuperMajorityDecider : UnanimousVoter
    {
        public Vote Decision;
        public int DecisionLevel;

        public SuperMajorityDecider(IUnit uc, IDag dag, IRandomSource rs, int commonVoteDeterministicPrefix, int zeroVoteRoundForCommonVote) : base(uc, dag, rs, commonVoteDeterministicPrefix, zeroVoteRoundForCommonVote)
        {
            Decision = Vote.Undecided;
            DecisionLevel = -1;
        }

        /// <summary>
        /// Decides if UC is popular (i.e. can be used as a timing unit). Returns (vote, level) on which the decision was made and current dag level.
        /// </summary>
        /// <param name="dagMaxLevel"></param>
        /// <returns></returns>
        public (Vote, int) DecideUnitIsPopular(int dagMaxLevel)
        {
            int decisionLevel = -1;

            if (Decision != Vote.Undecided)
            {
                return (Decision, decisionLevel);
            }
            var maxDecisionLevel = GetMaxDecideLevel(dagMaxLevel);

            for (var level = UC.Level() + FirstVotingRound + 1; level <= maxDecisionLevel; level++)
            {
                var decision = Vote.Undecided;

                var commonVote = LazyCommonVote(level);
                Dag.UnitsOnLevel(level).Iterate((primes) =>
                {
                    foreach (var v in primes)
                    {
                        var vDecision = Decide(v);
                        if (vDecision != Vote.Undecided && vDecision == commonVote())
                        {
                            decision = vDecision;
                            return false;
                        }
                    }
                    return true;
                });

                if (decision != Vote.Undecided)
                {
                    Decision = decision;
                    DecisionLevel = level;
                    Dispose();
                    return (decision, level);
                }
            }

            return (Vote.Undecided, -1);
        }

        /// <summary>
        /// Returns a maximal level of a prime unit which can be used for deciding, assuming that DAG is on level.
        /// </summary>
        /// <param name="dagMaxLevel"></param>
        /// <returns></returns>
        public int GetMaxDecideLevel(int dagMaxLevel)
        {
            var deterministicLevel = UC.Level() + (int)CommonVoteDeterministicPrefix;
            if (dagMaxLevel - 2 < deterministicLevel)
            {
                if (deterministicLevel > dagMaxLevel) return dagMaxLevel;
                return deterministicLevel;
            }

            return dagMaxLevel - 2;
        }

        public Vote Decide(IUnit u)
        {
            var commonVote = LazyCommonVote(u.Level() - 1);
            var votingResult = new VotingResult { Popular = 0, Unpopular = 0 };

            var result = VoteUsingPrimeAncestors(UC, u, Dag, (uc, uPrA) =>
            {
                var result = VoteUsing(uPrA);
                if (result == Vote.Undecided) result = commonVote();
                var updated = false;
                switch (result)
                {
                    case Vote.Popular:
                        votingResult.Popular++;
                        updated = true;
                        break;
                    case Vote.Unpopular:
                        votingResult.Unpopular++;
                        updated = true;
                        break;
                }

                if (updated)
                {
                    if (SuperMajority(Dag, votingResult) != Vote.Undecided) return (result, true);
                }
                else
                {
                    var test = votingResult;
                    ushort remaining = (ushort)(Dag.NProc() - uPrA.Creator() - 1);
                    test.Popular += remaining;
                    test.Unpopular += remaining;
                    if (SuperMajority(Dag, test) == Vote.Undecided) return (result, true);
                }

                return (result, false);
            });

            return SuperMajority(Dag, result);
        }
    }
}
