using AuremCore.Crypto.BN256.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Crypto.BN256
{
    public class VerificationKey
    {
        public G2 Key;

        public VerificationKey() { }

        public VerificationKey(G2 Key) {  this.Key = Key; }

        public VerificationKey(SecretKey k) { this.Key = new G2().ScalarBaseMult(k); }

        public VerificationKey(BigInteger k) { this.Key = new G2().ScalarBaseMult(new SecretKey(k)); }

        public bool Verify(Signature s, byte[] msg)
        {
            var p1 = new GT().Pair(s.Sig, Constants.G2Gen).Marshal();
            var p2 = new GT().Pair(G1.Hash(msg), Key).Marshal();

            return p1.SequenceEqual(p2);
        }
    }
}
