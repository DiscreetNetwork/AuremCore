using AuremCore.Crypto.Threshold;
using BN256Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Creating
{
    /// <summary>
    /// ShareDB is a simple storage system for shares in threshold signatures, indexed by the message they sign.
    /// </summary>
    public class ShareDB
    {
        public Config.Config Conf;
        public Dictionary<string, Dictionary<ushort, Share>> Data;

        public ShareDB(Config.Config conf)
        {
            Conf = conf;
            Data = new Dictionary<string, Dictionary<ushort, Share>>();
        }

        /// <summary>
        /// Puts the share that signs 'msg' into the storage. If there are enough shares for that msg, they are combined and the resulting signature is returned. Otherwise, returns null.
        /// </summary>
        /// <param name="share"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public Signature? Add(Share share, byte[] msg)
        {
            var key = Encoding.ASCII.GetString(msg);
            var success = Data.TryGetValue(key, out var shares);
            if (!success)
            {
                shares = new Dictionary<ushort, Share>();
                Data[key] = shares;
            }

            shares[share.Owner] = share;
            if (shares.Count >= Conf.WTKey.Threshold)
            {
                var shareArray = shares.Values.ToArray();
                (var sig, var ok) = Conf.WTKey.CombineShares(shareArray);
                if (ok)
                {
                    return sig;
                }
            }

            return null;
        }
    }
}
