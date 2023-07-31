using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Crypto.Signing
{
    public static class Keys
    {
        public static (SPublicKey pk, SPrivateKey sk) GenerateKeys()
        {
            var sdkey = new DiscreetCoreLib.Key(new byte[32]);
            var pdkey = new DiscreetCoreLib.Key(new byte[32]);
            DiscreetCoreLib.KeyOps.GenerateKeypair(ref sdkey, ref pdkey);

            return (new SPublicKey(pdkey.bytes), new SPrivateKey(sdkey.bytes));
        }
    }
}
