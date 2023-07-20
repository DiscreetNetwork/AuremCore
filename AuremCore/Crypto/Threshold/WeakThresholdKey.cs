using BN256Core;
using BN256Core.Models;
using AuremCore.Crypto.P2P;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Crypto.Threshold
{
    public class WeakThresholdKey : ThresholdKey
    {
        internal Dictionary<ushort, bool> shareProviders;

        public WeakThresholdKey() : base()
        {
            shareProviders = new Dictionary<ushort, bool>();
        }

        public Dictionary<ushort, bool> ShareProviders => shareProviders;

        public override Share? CreateShare(byte[] msg)
        {
            if (!shareProviders[owner]) return null;

            return new Share { owner = this.owner, sig = this.sk.Sign(msg) };
        }

        public static WeakThresholdKey Create(ThresholdKey[] tks, Dictionary<ushort, bool> shareProviders)
        {
            int n = tks[0].vks.Length;

            WeakThresholdKey key = new WeakThresholdKey { globalVK = new VerificationKey(), sk = new SecretKey(), owner = tks[0].owner, threshold = tks[0].threshold, vks = new VerificationKey[n], shareProviders = shareProviders };

            for (int i = 0; i < n; i++)
            {
                key.vks[i] = new VerificationKey();
            }

            foreach (var tk in tks)
            {
                key.sk.Add(tk.sk);
                key.globalVK = VerificationKey.Add(key.globalVK, tk.globalVK);
                for (int i = 0; i < tk.vks.Length; i++)
                {
                    key.vks[i] = VerificationKey.Add(key.vks[i], tk.vks[i]);
                }
            }

            return key;
        }

        public static WeakThresholdKey Seeded(ushort nproc, ushort pid, int seed, Dictionary<ushort, bool> shareProviders)
        {
            Random rnd = new Random(seed);
            var threshold = TUtil.MinimalTrusted(nproc);

            var coeffs = new BigInteger[threshold];
            for (int i = 0; i < threshold; i++)
            {
                coeffs[i] = SecretKey.RandomScalar();
            }

            var skeys = new P2PSecretKey[nproc];
            var pkeys = new P2PPublicKey[nproc];
            for (int i = 0; i < nproc; i++)
            {
                (pkeys[i], skeys[i]) = P2P.P2P.GenerateKeys();
            }
            ushort dealer = 0;

            var p2pkeys = P2P.P2P.Keys(skeys[dealer], pkeys, dealer);

            var gtk = TSS.Create(nproc, coeffs);
            var tkenc = gtk.Encrypt(p2pkeys);
            (var tk, _) = ThresholdKey.Decode(tkenc.Encode(), dealer, pid, p2pkeys[pid]);

            if (shareProviders == null)
            {
                shareProviders = new();
                for (ushort i = 0; i < nproc; i++)
                {
                    shareProviders[i] = true;
                }
            }

            return Create(new ThresholdKey[] { tk }, shareProviders);
        }
    }
}
