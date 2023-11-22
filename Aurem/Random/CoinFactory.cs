using Aurem.Model;
using AuremCore.Crypto.Threshold;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Random
{
    public class CoinFactory : IRandomSourceFactory
    {
        public ushort Pid;
        public WeakThresholdKey WTKey;

        public CoinFactory(ushort pid, WeakThresholdKey wtkey)
        {
            Pid = pid;
            WTKey = wtkey;
        }

        public CoinFactory(ushort nproc, ushort pid, int seed, Dictionary<ushort, bool> shareProviders)
        {
            var wtk = WeakThresholdKey.Seeded(nproc, pid, seed, shareProviders);
            Pid = pid;
            WTKey = wtk;
        }

        public IRandomSource CreateRandomSource(IDag dag)
        {
            return new Coin(Pid, dag, WTKey, WTKey.ShareProviders);
        }

        public (byte[], Exception?) DealingData(uint epoch)
        {
            if (WTKey.ShareProviders[Pid])
            {
                return (WTKey.CreateShare(Coin.Nonce(0, epoch))!.Marshal(), null);
            }

            return (null!, null);
        }
    }
}
