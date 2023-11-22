using DiscreetCoreLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremBlockAuthority.Core.Crypto
{
    public class Sig
    {
        private readonly byte[] data;

        public Sig()
        {

        }

        public Sig(byte[] data)
        {
            this.data = data;
        }

        public Sig(Signature sig)
        {
            data = sig.Serialize()[0..64];
        }

        public Sig(ReadOnlySpan<byte> data)
        {
            this.data = data[0..64].ToArray();
        }

        public void Marshal(Stream s)
        {
            s.Write(data);
        }

        public byte[] Serialize()
        {
            var rv = new byte[data.Length];
            Buffer.BlockCopy(data, 0, rv, 0, data.Length);
            return rv;
        }

        public bool Verify(PubKey k, byte[] msg)
        {
            if (k == null) return false;
            if (msg == null) return false;

            var sig = new Signature(new Key(data[0..32]), new Key(data[32..64]), k._key, true);
            return sig.Verify(msg);
        }
    }
}
