using Aurem.Model;
using AuremCore.Core.Extensions;
using AuremCore.Crypto.Encrypt;
using AuremCore.Crypto.P2P;
using AuremCore.Crypto.Threshold;
using BN256Core;
using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Random
{
    public class Beacon : IRandomSource, IRandomSourceFactory
    {
        public ushort Pid;
        public Config.Config Conf;
        public IDag? Dag;
        public WTKData[] Wtk;
        public ThresholdKey[] Tks;
        public Vote[,] Votes;
        public Dictionary<ushort, bool>[] ShareProviders;
        public Dictionary<ushort, bool>[] Subcoins;

        public ConcurrentDictionary<Hash, Share>[] Shares;
        public PolyVerifier PolyVerifier;
        public SymmetricKey[] P2PKeys;

        public Beacon(Config.Config conf)
        {
            P2PKeys = P2P.Keys(conf.P2PSecretKey, conf.P2PPublicKeys.ToArray(), conf.Pid);
            Pid = conf.Pid;
            Conf = conf;
            Wtk = new WTKData[conf.NProc];
            Tks = new ThresholdKey[conf.NProc];
            Votes = new Vote[conf.NProc, conf.NProc];
            ShareProviders = new Dictionary<ushort, bool>[conf.NProc];
            Subcoins = new Dictionary<ushort, bool>[conf.NProc];
            Shares = new ConcurrentDictionary<Hash, Share>[conf.NProc];
            PolyVerifier = PolyVerifier.New(conf.NProc, IDag.MinimalTrusted(conf.NProc));

            for (int i = 0; i < conf.NProc; i++)
            {
                Shares[i] = new ConcurrentDictionary<Hash, Share>();
                Subcoins[i] = new Dictionary<ushort, bool>();
            }
        }

        public IRandomSource CreateRandomSource(IDag dag)
        {
            dag.AddCheck((u, dag) => {
                var ex = CheckCompliance(u, dag);
                if (ex != null) throw ex;
            });
            dag.BeforeInsert(Update);
            Dag = dag;
            return this;
        }

        public byte[] RandomBytes(ushort pid, int level)
        {
            if (level < RandomConstants.SharesLevel)
            {
                // asked on too low a level
                return null!;
            }

            if (Interlocked.Read(ref Wtk[pid].Stored) == 0)
            {
                // wtk for this pid not received yet, or still processing
                return null!;
            }

            var wtk = Wtk[pid].Wtk;
            var shares = new List<Share>();
            var units = UnitsOnLevel(Dag!, level);

            foreach (var u in units)
            {
                if (ShareProviders[pid][u.Creator()])
                {
                    var ushares = new List<Share>();
                    foreach (var sc in Subcoins[pid].Keys)
                    {
                        ushares.Add(Shares[sc][u.Hash()]);
                    }

                    shares.Add(Share.SumShares(ushares));
                }
            }

            (var coin, var ok) = wtk.CombineShares(shares.ToArray());
            if (!ok)
            {
                // not enough shares
                return null!;
            }

            return coin!.Marshal();
        }

        public Exception? CheckCompliance(IUnit u, IDag dag)
        {
            if (u.Level() == RandomConstants.DealingLevel)
            {
                var tcEncoded = u.RandomSourceData();
                (var tc, _) = DelegateExtensions.InvokeAndCaptureException(ThresholdKey.Decode, tcEncoded, u.Creator(), Pid, P2PKeys[u.Creator()], out var err);
                if (err != null)
                {
                    return err;
                }

                if (!tc.PolyVerify(PolyVerifier))
                {
                    return new Exception("Tcoin does not come from a polynomial sequence");
                }

                return null;
            }

            if (u.Level() == RandomConstants.VotingLevel)
            {
                (var votes, var err) = Vote.UnmarshalVotes(u.RandomSourceData(), Conf.NProc);
                if (err != null) return err;

                err = ValidateVotes(u, votes);
                if (err != null) return err;

                return null;
            }

            if (u.Level() >= RandomConstants.SharesLevel)
            {
                (var shares, var err) = Vote.UnmarshalShares(u.RandomSourceData(), Conf.NProc);
                if (err != null) return err;

                for (ushort pid = 0; pid < Conf.NProc; pid++)
                {
                    if (Votes[u.Creator(), pid] != null && Votes[u.Creator(), pid].IsCorrect())
                    {
                        if (shares[pid] == null)
                        {
                            return new Exception("missing share");
                        }

                        // slow
                        if (Tks[pid].VerifyShare(shares[pid], Nonce(u.Level())))
                        {
                            return new Exception("invalid share");
                        }
                    }
                }

                return null;
            }

            return null;
        }

        private void Update(IUnit u)
        {
            if (u.Level() == RandomConstants.DealingLevel)
            {
                var tcEncoded = u.RandomSourceData();
                (var tc, var okSecretKey) = DelegateExtensions.InvokeAndCaptureException(ThresholdKey.Decode, tcEncoded, u.Creator(), Pid, P2PKeys[u.Creator()], out var ex);
                if (!okSecretKey)
                {
                    var secret = new SharedSecret(Conf.P2PSecretKey, Conf.P2PPublicKeys[u.Creator()]);
                    Votes[Pid, u.Creator()] = new Vote(secret);
                }
                else
                {
                    Votes[Pid, u.Creator()] = new Vote(null!);
                }

                Tks[u.Creator()] = tc;
            }

            if (u.Level() == RandomConstants.VotingLevel)
            {
                (var votes, _) = Vote.UnmarshalVotes(u.RandomSourceData(), Conf.NProc);
                for (int pid = 0; pid < votes.Length; pid++)
                {
                    Votes[u.Creator(), pid] = votes[pid];
                }
            }

            if (u.Level() == RandomConstants.MultikeyLevel)
            {
                var coinsApprovedBy = new ushort[Conf.NProc];
                var nBelowUOnVotingLevel = (ushort)0;
                var providers = new Dictionary<ushort, bool>();
                var votingUnits = UnitsOnLevel(Dag!, RandomConstants.VotingLevel);

                foreach (var v in votingUnits)
                {
                    if (IUnit.Above(u, v))
                    {
                        providers[v.Creator()] = true;
                        nBelowUOnVotingLevel++;
                        for (ushort pid = 0; pid < Conf.NProc; pid++)
                        {
                            if (Votes[v.Creator(), pid] != null && Votes[v.Creator(), pid].IsCorrect())
                            {
                                coinsApprovedBy[pid]++;
                            }
                        }
                    }
                }

                var coinsToMerge = new List<ThresholdKey>();
                for (ushort pid = 0; pid < Conf.NProc; pid++)
                {
                    if (coinsApprovedBy[pid] == nBelowUOnVotingLevel)
                    {
                        coinsToMerge.Add(Tks[pid]);
                        Subcoins[u.Creator()][pid] = true;
                    }
                }

                Wtk[u.Creator()] = new WTKData { Wtk = WeakThresholdKey.Create(coinsToMerge.ToArray(), providers) };
                ShareProviders[u.Creator()] = providers;
                Interlocked.Exchange(ref Wtk[u.Creator()].Stored, 1);
            }

            if (u.Level() >= RandomConstants.SharesLevel)
            {
                (var shares, _) = Vote.UnmarshalShares(u.RandomSourceData(), Conf.NProc);
                for (ushort pid = 0; pid < Conf.NProc; pid++)
                {
                    Shares[pid][u.Hash()] = shares[pid];
                }
            }
        }

        private Exception? ValidateVotes(IUnit u, Vote[] votes)
        {
            var dealingUnits = UnitsOnLevel(Dag, RandomConstants.DealingLevel);
            var createdDealing = new bool[Conf.NProc];

            foreach (var v in dealingUnits)
            {
                var shouldVote = IUnit.Above(u, v);
                if (shouldVote && votes[v.Creator()] == null) return new Exception("missing vote");

                if (!shouldVote && votes[v.Creator()] != null) return new Exception("vote on dealing unit not below the unit");

                if (shouldVote && !votes[v.Creator()].IsCorrect())
                {
                    if (!VerifyWrongSecretKeyProof(u.Creator(), v.Creator(), votes[v.Creator()].Proof!))
                    {
                        return new Exception("the provided proof is incorrect");
                    }
                }

                createdDealing[v.Creator()] = true;
            }

            for (int pid = 0; pid < createdDealing.Length; pid++)
            {
                if (votes[pid] != null && !createdDealing[pid])
                {
                    return new Exception("vote on non-existing dealing unit");
                }
            }

            return null;
        }

        private bool VerifyWrongSecretKeyProof(ushort prover, ushort suspect, SharedSecret proof)
        {
            if (!proof.Verify(Conf.P2PPublicKeys[prover], Conf.P2PPublicKeys[suspect]))
            {
                return false;
            }

            var key = DelegateExtensions.InvokeAndCaptureException(proof.Key, out var ex);
            if (ex != null)
            {
                return false;
            }

            return Tks[suspect].CheckSecretKey(prover, key);
        }

        public (byte[], Exception?) DealingData(uint epoch)
        {
            if (epoch != 0)
            {
                return (null!, new Exception("Beacon was asked for dealing data with non-zero epoch"));
            }

            var gtc = TSS.CreateRandom(Conf.NProc, IDag.MinimalTrusted(Conf.NProc), out _);
            var tc = DelegateExtensions.InvokeAndCaptureException(gtc.Encrypt, P2PKeys, out var err);
            if (err != null)
            {
                return (null!, err);
            }

            return (tc.Encode(), null);
        }

        public (byte[], Exception?) DataToInclude(List<IUnit> parents, int level)
        {
            if (level == RandomConstants.VotingLevel)
            {
                return (Vote.MarshalVotes(Votes, Pid), null);
            }

            if (level >= RandomConstants.SharesLevel)
            {
                var shares = new Share[Conf.NProc];
                for (ushort pid = 0; pid < Conf.NProc; pid++)
                {
                    if (Votes[Conf.Pid, pid] != null && Votes[Conf.Pid, pid].IsCorrect())
                    {
                        shares[pid] = Tks[pid].CreateShare(Nonce(level))!;
                    }
                }

                return (Vote.MarshalShares(shares), null);
            }

            return (Array.Empty<byte>(), null);
        }

        public WeakThresholdKey GetWTK(ushort head)
        {
            return Wtk[head].Wtk;
        }

        private static List<IUnit> UnitsOnLevel(IDag dag, int level)
        {
            var res = new List<IUnit>();
            var su = dag.UnitsOnLevel(level);

            su?.Iterate((us) =>
            {
                if (us.Count > 0) res.Add(us.First());
                return true;
            });

            return res;
        }

        public static byte[] Nonce(int level)
        {
            var buf = new byte[2];
            BinaryPrimitives.WriteUInt16LittleEndian(buf, (ushort)level);
            return buf;
        }
    }
}
