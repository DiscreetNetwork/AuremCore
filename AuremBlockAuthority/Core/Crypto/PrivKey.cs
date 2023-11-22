using DiscreetCoreLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremBlockAuthority.Core.Crypto
{
    public class PrivKey
    {
        internal Key _key;

        public PrivKey(byte[] data)
        {
            _key = new Key(data);
        }

        public PrivKey(Key key)
        {
            _key = key;
        }

        public Sig Sign(byte[] msg, PubKey pk)
        {
            return new Sig(new Signature(_key, pk._key, msg, true));
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
