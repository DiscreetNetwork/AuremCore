using BN256Core.Extensions;
using BN256Core.Models;
using AuremCore.Crypto.Encrypt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Crypto.P2P
{
    public class SharedSecret
    {
        internal G1 secret;

        public SharedSecret() { }
        public SharedSecret(G1 secret) {  this.secret = secret; }

        public SharedSecret(P2PSecretKey sk, P2PPublicKey pk)
        {
            secret = new G1().ScalarMult(pk.g1, sk.k);
        }

        public byte[] Marshal()
        {
            return secret.Marshal();
        }

        public SharedSecret Unmarshal(byte[] data)
        {
            secret = new G1().Unmarshal(data);
            return this;
        }

        public bool Verify(P2PPublicKey pk1, P2PPublicKey pk2)
        {
            var p1 = new GT().Pair(secret, P2P.GenG2).Marshal();
            var p2 = new GT().Pair(pk1.g1, pk2.g2).Marshal();

            return p1.BEquals(p2);
        }

        public SymmetricKey Key()
        {
            return new SymmetricKey(Marshal());
        }
    }
}
