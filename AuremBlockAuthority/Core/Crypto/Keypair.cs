using DiscreetCoreLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremBlockAuthority.Core.Crypto
{
    public class Keypair
    {
        private PubKey _pub;
        private PrivKey _priv;

        public PubKey Pub { get { return _pub; } }
        public PrivKey Priv { get { return _priv; } }

        public Keypair(PubKey pub, PrivKey priv)
        {
            _pub = pub;
            _priv = priv;
        }

        public Keypair(ReadOnlySpan<byte> data)
        {
            _pub = new PubKey(data[0..32].ToArray());
            _priv = new PrivKey(data[32..64].ToArray());
        }

        public void Marshal(Stream s)
        {
            _pub.Marshal(s);
            _priv.Marshal(s);
        }

        public byte[] Marshal()
        {
            var dat = new byte[64];
            Array.Copy(_pub.Marshal(), dat, 32);
            Array.Copy(_priv.Marshal(), 0, dat, 32, 32);

            return dat;
        }

        public static Keypair New()
        {
            (Key x, Key xG) = KeyOps.GenerateKeypair();
            var xk = new PrivKey(x);
            var xkG = new PubKey(xG);

            return new Keypair(xkG, xk);
        }

        public Sig Sign(byte[] msg)
        {
            return _priv.Sign(msg, _pub);
        }

        public bool VerifySignature(Sig s, byte[] msg)
        {
            return s.Verify(_pub, msg);
        }
    }
}
