using Aurem.Model;
using AuremCore.Crypto.Threshold;
using AuremCore.FastLogger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Creating
{
    /// <summary>
    /// Implements <see cref="IEpochProofBuilder"/>.
    /// </summary>
    public class EpochProofBuilder : IEpochProofBuilder
    {
        public Config.Config Conf;
        public uint Epoch;
        public ShareDB Shares;
        public Logger Log;

        private EpochProofBuilder(Config.Config conf, Logger log, uint epoch)
        {
            Conf = conf;
            Epoch = epoch;
            Shares = new(conf);
            Log = log;
        }

        public static Func<uint, EpochProofBuilder> Factory(Config.Config conf, Logger log)
        {
            return epoch => new EpochProofBuilder(conf, log, epoch);
        }

        public byte[] BuildShare(IUnit lastTimingUnit)
        {
            var msg = ProofUtil.EncodeProof(lastTimingUnit);
            var share = Conf.WTKey.CreateShare(msg);
            if (share != null) return ProofUtil.EncodeShare(share, msg);

            return Array.Empty<byte>();
        }

        public bool Verify(IPreunit pu)
        {
            if (Epoch + 1 != pu.EpochID()) return false;

            return ProofUtil.EpochProof(pu, Conf.WTKey);
        }

        /// <summary>
        /// Extracts threshold signature shares from finishing units.
        /// If there are enough shares to combine, it produces the signature and converts it to a byte array. Otherwise, null is returned.
        /// </summary>
        /// <param name="u"></param>
        /// <returns></returns>
        public byte[] TryBuilding(IUnit u)
        {
            // ignore regular units and finishing units with empty data
            if (u.Level() < Conf.OrderStartLevel + Conf.EpochLength || u.Data().Length == 0)
            {
                return null;
            }

            Share share;
            byte[] msg;
            try
            {
                (share, msg) = ProofUtil.DecodeShare(u.Data());
            }
            catch (Exception ex)
            {
                Log.Error().Str("where", "creator.DecodeShare").Msg(ex.Message);
                return null;
            }

            if (!Conf.WTKey.VerifyShare(share, msg))
            {
                Log.Error().Str("where", "creator.VerifyShare").Msg("failed to verify share");
            }

            var sig = Shares.Add(share, msg);
            if (sig != null)
            {
                Log.Info().Str("where", "creator.TryBuilding").Msg("successfully built a proof of the epoch");
                return ProofUtil.EncodeSignature(sig, msg);
            }

            return null;
        }
    }
}
