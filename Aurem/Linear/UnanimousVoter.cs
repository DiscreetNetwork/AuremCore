using Aurem.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Linear
{
    public class UnanimousVoter
    {
        public IDag Dag { get; set; }
        public IRandomSource RS { get; set; }
        public IUnit UC { get; set; }
        public int ZeroVoteRoundForCommonVote { get; set; }
        public int CommonVoteDeterministicPrefix { get; set; }
        public Dictionary<Hash, Vote> VotingMemo { get; set; }

        public static readonly int FirstVotingRound = 1;

        public UnanimousVoter(IUnit uc, IDag dag, IRandomSource rs, int commonVoteDeterministicPrefix, int zeroVoteRoundForCommonVote)
        {
            UC = uc;
            Dag = dag;
            RS = rs;
            ZeroVoteRoundForCommonVote = zeroVoteRoundForCommonVote;
            CommonVoteDeterministicPrefix = commonVoteDeterministicPrefix;
            VotingMemo = new();
        }

        public Vote VoteUsing(IUnit u)
        {
            Vote result = Vote.Undecided;

            var r = u.Level() - UC.Level();
            if (r < FirstVotingRound) return Vote.Undecided;

            bool success = VotingMemo.TryGetValue(u.Hash(), out var _cachedResult);
            if (success) return _cachedResult;

            try
            {
                if (r == FirstVotingRound) return InitialVote(UC, u);

                var commonVote = LazyCommonVote(u.Level() - 1);
                Vote? lastVote = null;
                VoteUsingPrimeAncestors(UC, u, Dag, (uc, uPrA) =>
                {
                    result = VoteUsing(uPrA);
                    if (result == Vote.Undecided) result = commonVote();
                    if (lastVote != null)
                    {
                        if (lastVote.Value != result)
                        {
                            lastVote = Vote.Undecided;
                            return (result, true);
                        }
                    }
                    else
                    {
                        lastVote = result;
                    }

                    return (result, false);
                });

                if (lastVote == null)
                {
                    return Vote.Undecided;
                }

                return lastVote.Value;
            }
            finally
            {
                VotingMemo[u.Hash()] = result;
            }
        }

        public Vote InitialVote(IUnit uc, IUnit u) => IUnit.Above(u, uc) ? Vote.Popular : Vote.Unpopular;

        public void Dispose()
        {
            VotingMemo.Clear();
        }

        public Func<Vote> LazyCommonVote(int level)
        {
            var initialized = false;
            Vote CommonVoteValue = Vote.Undecided;
            return () =>
            {
                if (!initialized)
                {
                    CommonVoteValue = CommonVote(level);
                    initialized = true;
                }

                return CommonVoteValue;
            };
        }

        /// <summary>
        /// Tosses a coin using a given <see cref="IRandomSource"/>.
        /// </summary>
        /// <param name="uc">The unit whose popularity decision is being considered by tossing the coin.</param>
        /// <param name="level">The level for which we are tossing the coin.</param>
        /// <param name="rs">The random source to use for the coin toss.</param>
        /// <returns>A pseudorandom bit impossible to predict before the level is reached: <see langword="true"/> for 1, <see langword="false"/> for 0.</returns>
        public static bool CoinToss(IUnit uc, int level, IRandomSource rs)
        {
            return (rs.RandomBytes(uc.Creator(), level)[0] & 1) == 0;
        }

        public Vote CommonVote(int level)
        {
            var round = level - UC.Level();
            if (round <= FirstVotingRound)
            {
                // Default vote is asked on too low of a unit level
                return Vote.Undecided;
            }

            if (round <= CommonVoteDeterministicPrefix)
            {
                if (round == ZeroVoteRoundForCommonVote) return Vote.Unpopular;
                return Vote.Popular;
            }

            return CoinToss(UC, level + 1, RS) ? Vote.Popular : Vote.Unpopular;
        }

        /// <summary>
        /// Checks if votes for popular or unpopular make a quorum.
        /// </summary>
        /// <param name="dag"></param>
        /// <param name="votes"></param>
        /// <returns>The vote making a quorum or undecided if there is no quorum.</returns>
        public static Vote SuperMajority(IDag dag, VotingResult votes) => dag.IsQuorum(votes.Popular) ? Vote.Popular : (dag.IsQuorum(votes.Unpopular) ? Vote.Unpopular : Vote.Undecided);

        public static VotingResult VoteUsingPrimeAncestors(IUnit uc, IUnit u, IDag dag, Func<IUnit, IUnit, (Vote, bool)> voter)
        {
            var votingResult = new VotingResult { Popular = 0, Unpopular = 0 };

            var parents = u.Parents().ToArray();
            for (int pid = 0; pid < parents.Length; pid++)
            {
                var floor = u.Floor((ushort)pid);
                var votesOne = false;
                var votesZero = false;
                var finish = false;

                foreach (var _v in floor)
                {
                    var v = _v;
                    // find prime ancestor
                    for (var predecessor = v; predecessor.Level() >= u.Level() -1;)
                    {
                        v = predecessor;
                        predecessor = v.Predecessor();
                        if (predecessor == null) break;
                    }

                    if (v.Level() != u.Level() - 1) continue;

                    var vote = Vote.Undecided;
                    (vote, finish) = voter(uc, v);

                    switch (vote)
                    {
                        case Vote.Popular:
                            votesOne = true; break;
                        case Vote.Unpopular:
                            votesZero = true; break;
                    }

                    if (finish || (votesOne && votesZero)) break;
                }

                if (votesOne) votingResult.Popular++;
                if (votesZero) votingResult.Unpopular++;
                if (finish) return votingResult;
            }

            return votingResult;
        }
    }
}
