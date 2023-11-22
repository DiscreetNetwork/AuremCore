using DiscreetCoreLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremBlockAuthority.Core.Crypto
{
    public class PubKey
    {
        internal Key _key;

        public PubKey(byte[] data)
        {
            _key = new Key(data);
        }

        public PubKey(Key key)
        {
            _key = key;
        }

        public bool VerifySignature(Sig sig, byte[] msg)
        {
            return sig.Verify(this, msg);
        }

        public byte[] Marshal()
        {
            return _key.bytes;
        }

        public void Marshal(Stream s)
        {
            s.Write(_key.bytes);
        }
    }
}
