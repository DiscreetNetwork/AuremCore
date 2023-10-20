using BN256Core.Extensions;
using BN256Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BN256Core
{
    public class VerificationKey
    {
        public G2 Key;

        public VerificationKey() { Key = new(); }

        public VerificationKey(G2 Key) {  this.Key = Key; }

        public VerificationKey(SecretKey k) { this.Key = new G2().ScalarBaseMult(k); }

        public VerificationKey(BigInteger k) { this.Key = new G2().ScalarBaseMult(new SecretKey(k)); }

        public byte[] Marshal() => Key.Marshal();
        public VerificationKey Unmarshal(byte[] data)
        {
            Key.Unmarshal(data);
            return this;
        }

        public VerificationKey Unmarshal(ReadOnlySpan<byte> data)
        {
            Key.Unmarshal(data);
            return this;
        }

        public bool Verify(Signature s, ReadOnlySpan<byte> msg)
        {
            var p1 = new GT().Pair(s.Sig, Constants.G2Gen).Marshal();
            var p2 = new GT().Pair(G1.Hash(msg), Key).Marshal();

            return p1.BEquals(p2);
        }

        public static (VerificationKey, SecretKey) GenerateKeys()
        {
            var sk = new SecretKey(SecretKey.RandomScalar());
            return (sk.VerificationKey(), sk);
        }

        public static VerificationKey Add(VerificationKey v1, VerificationKey v2)
        {
            if (v1 == null) return v2;

            return new VerificationKey(new G2().Add(v1.Key, v2.Key));
        }

        public static bool VerifyKeys(VerificationKey v, SecretKey s)
        {
            var v2 = s.VerificationKey();
            return v.Marshal().BEquals(v2.Marshal());
        }

        public string Encode() => Convert.ToBase64String(Marshal());

        public static VerificationKey DecodeVerificationKey(string enc)
        {
            if (enc == null) throw new ArgumentNullException(nameof(enc));

            var data = Convert.FromBase64String(enc);
            return new VerificationKey().Unmarshal(data);
        }
    }
}
