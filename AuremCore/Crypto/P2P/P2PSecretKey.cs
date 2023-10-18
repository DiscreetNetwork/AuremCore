using BN256Core;
using BN256Core.Models;
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

        public P2PSecretKey() { }

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

        public static P2PSecretKey Decode(string enc)
        {
            return new P2PSecretKey().Unmarshal(Convert.FromBase64String(enc));
        }

        // utility methods to ensure network traffic cannot be faked
        public Signature Sign(byte[] data)
        {
            Signature sig = k.Sign(data);
            return sig;
        }
    }
}
