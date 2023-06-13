using AuremCore.Crypto.BN256;
using AuremCore.Crypto.BN256.Models;
using AuremCore.Crypto.Encrypt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Crypto.P2P
{
    public class P2PSecretKey
    {
        internal SecretKey k;

        public P2PSecretKey(SecretKey k)
        {
            this.k = k;
        }

        public byte[] Marshal()
        {
            return k.Marshal();
        }

        public P2PSecretKey Unmarshal(byte[] data)
        {
            k = new SecretKey().Unmarshal(data);
            return this;
        }

        public string Encode()
        {
            return Convert.ToBase64String(Marshal());
        }

        public P2PSecretKey Decode(string enc)
        {
            return Unmarshal(Convert.FromBase64String(enc));
        }
    }
}
