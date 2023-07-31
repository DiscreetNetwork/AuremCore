using Aurem.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Crypto.Signing
{
    public class SPublicKey : IPublicKey
    {
        private readonly byte[] keyData;

        public SPublicKey(byte[] keyData)
        {
            this.keyData = keyData;
        }

        public bool Verify(IPreunit preunit)
        {
            var sig = new DiscreetCoreLib.Signature(preunit.Signature().Concat(keyData).ToArray());
            return sig.Verify(preunit.Hash().Data, false);
        }

        public string Encode() => Convert.ToBase64String(keyData);

        public static SPublicKey DecodePublicKey(string enc)
        {
            if (enc == null) throw new ArgumentNullException(nameof(enc));

            var data = Convert.FromBase64String(enc);
            if (data.Length != 32) throw new FormatException("bad encoded private key");

            return new SPublicKey(data);
        }
    }
}
