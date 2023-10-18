using Aurem.Model;
using AuremCore.Core.Extensions;
using AuremCore.Crypto.Threshold;
using BN256Core;
using BN256Core.Extensions;
using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Random
{
    public class Coin : IRandomSource
    {
        public ushort Pid;
        public IDag Dag;
        public WeakThresholdKey Wtk;
        public ConcurrentDictionary<Hash, Share> CoinShares;
        public Dictionary<ushort, bool> ShareProviders;
        public ConcurrentDictionary<int, byte[]> RBytes;

        public Coin(ushort pid, IDag dag, WeakThresholdKey wtk, Dictionary<ushort, bool> shareProviders)
        {
            Pid = pid;
            Dag = dag;
            Wtk = wtk;
            CoinShares = new ConcurrentDictionary<Hash, Share>();
            ShareProviders = shareProviders;
            RBytes = new ConcurrentDictionary<int, byte[]>();

            dag.AddCheck((u, dag) =>
            {
                var exc = CheckCompliance(u, dag);
                if (exc != null) throw exc;
            });
            dag.BeforeInsert(Update);
        }

        public static byte[] Nonce(int level, uint epoch)
        {
            var data = new byte[8];
            BinaryPrimitives.WriteUInt64LittleEndian(data, ((ulong)epoch << 16) + (ulong)level);
            return data;
        }

        public byte[] RandomBytes(ushort pid, int level)
        {
            var success = RBytes.TryGetValue(level, out var rv);
            return success ? rv! : null!;
        }

        private void Update(IUnit u)
        {
            ShareProviders.TryGetValue(u.Creator(), out var v1);
            if (v1)
            {
                var cs = new Share();
                var offset = Constants.SignatureLength;
                if (u.Dealing())
                {
                    // dealing units don't contain random data from previous level
                    offset = 0;
                }

                cs.Unmarshal(u.RandomSourceData()[offset..]);
                CoinShares[u.Hash()] = cs;
            }

            if (!u.Dealing())
            {
                if (RBytes.Count == u.Level() - 1)
                {
                    byte[] toAdd = new byte[Constants.SignatureLength];
                    Array.Copy(u.RandomSourceData(), 0, toAdd, 0, Constants.SignatureLength);
                    RBytes.AddOrUpdate(u.Level() - 1, toAdd, (k, v) => v);
                }
            }
        }

        private Exception? CheckCompliance(IUnit u, IDag dag)
        {
            ShareProviders.TryGetValue(u.Creator(), out var v0);
            if (u.Dealing() && v0)
            {
                DelegateExtensions.InvokeAndCaptureException(new Share().Unmarshal, u.RandomSourceData(), out var exc);
                return exc;
            }

            if (!u.Dealing())
            {
                if (u.RandomSourceData().Length < Constants.SignatureLength)
                {
                    return new Exception("random source data too short");
                }

                var uRandomBytes = u.RandomSourceData()[0..Constants.SignatureLength];
                var sucess = RBytes.TryGetValue(u.Level() - 1, out var rb);
                if (rb != null)
                {
                    if (!rb.BEquals(uRandomBytes)) return new Exception("incorrect random bytes");
                }
                else
                {
                    var coin = new Signature();
                    DelegateExtensions.InvokeAndCaptureException(coin.Unmarshal, uRandomBytes, out var exc);
                    if (exc != null) return exc;

                    if (!Wtk.VerifySignature(coin, Nonce(u.Level()-1, u.EpochID())))
                    {
                        return new Exception("incorrect random bytes");
                    }
                }

                ShareProviders.TryGetValue(u.Creator(), out var v1);
                if (v1)
                {
                    var err = DelegateExtensions.InvokeAndCaptureException(new Share().Unmarshal, u.RandomSourceData()[Constants.SignatureLength..], out var exc);
                    if (exc != null) return exc;
                }

                return null;
            }

            if (u.RandomSourceData() != null) return new Exception("random source data should be empty");

            return null;
        }

        public (byte[], Exception?) DataToInclude(List<IUnit> parents, int level)
        {
            if (level == 0) throw new Exception("Aurem.Random.Coin.DataToInclude: called on a dealing unit");

            using var ms = new MemoryStream();
            var success = RBytes.TryGetValue(level - 1, out var rb1);
            if (success)
            {
                ms.Write(rb1.AsSpan()[0..(Constants.SignatureLength > rb1!.Length ? rb1.Length : Constants.SignatureLength)]);
            }
            else
            {
                (var rb, var err) = CombineShares(level - 1);
                if (err != null) return (null!, err);

                if (RBytes.Count == level - 1)
                {
                    byte[] toAdd = new byte[rb.Length];
                    Array.Copy(rb, 0, toAdd, 0, rb.Length);
                    RBytes.AddOrUpdate(level - 1, toAdd, (k, v) => v);
                }

                ms.Write(rb);
            }

            ShareProviders.TryGetValue(Pid, out var v1);
            if (v1)
            {
                ms.Write(Wtk.CreateShare(Nonce(level, Dag.EpochID()))!.Marshal());
            }

            return (ms.ToArray(), null);
        }

        private (byte[], Exception?) CombineShares(int level)
        {
            var shares = new List<Share>();
            var shareCollected = new Dictionary<ushort, bool>();

            var su = Dag.UnitsOnLevel(level);
            if (su == null)
            {
                return (null!, new Exception("no primes on a given level"));
            }

            su.Iterate((us) =>
            {
                foreach (var v in us)
                {
                    ShareProviders.TryGetValue(v.Creator(), out var v1);
                    shareCollected.TryGetValue(v.Creator(), out var v2);

                    if (!v1 || v2) continue;

                    var success = CoinShares.TryGetValue(v.Hash(), out var cs);
                    if (success)
                    {
                        shares.Add(cs!);
                        shareCollected[v.Creator()] = true;
                        if (shares.Count >= Wtk.Threshold) return false;

                        return true;
                    }
                }

                return true;
            });

            (var coin, var ok) = Wtk.CombineShares(shares.ToArray());
            if (!ok)
            {
                return (null!, new Exception("combining shares failed"));
            }

            if (!Wtk.VerifySignature(coin!, Nonce(level, Dag.EpochID())))
            {
                return (null!, new Exception("verification of coin failed"));
            }

            return (coin!.Marshal(), null);
        }
    }
}
