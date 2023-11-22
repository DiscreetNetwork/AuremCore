using Aurem.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Crypto.Signing
{
    public class SPrivateKey : IPrivateKey
    {
        private readonly byte[] keyData;

        public SPrivateKey(byte[] keyData)
        {
            this.keyData = keyData;
        }

        public static SPrivateKey DecodePrivateKey(string enc)
        {
            if (enc == null) throw new ArgumentNullException(nameof(enc));

            var data = Convert.FromBase64String(enc);
            if (data.Length != 32) throw new FormatException("bad encoded private key");

            return new SPrivateKey(data);
        }

        public string Encode() => Convert.ToBase64String(keyData);

        public byte[] Sign(Hash h) {
            var sdkey = new DiscreetCoreLib.Key(keyData);
            var pdkey = DiscreetCoreLib.KeyOps.ScalarmultBase(ref sdkey);

            return new DiscreetCoreLib.Signature(sdkey, pdkey, h.Data, false).Serialize()[0..64];
        }
    }
}
