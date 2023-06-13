using AuremCore.Crypto.BN256.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Crypto.BN256
{
    public class Signature
    {
        public G1 Sig;

        public Signature() {  Sig = new G1(); }

        public Signature(G1 sig) {  Sig = sig; }

        public byte[] Marshal() => Sig.Marshal();

        public Signature Unmarshal(ReadOnlySpan<byte> bytes)
        {
            Sig.Unmarshal(bytes);
            return this;
        }

        public static Signature Add(Signature s1, Signature s2)
        {
            if (s1 == null) return s2;

            return new Signature(new G1().Add(s1.Sig, s2.Sig));
        }

        public static Signature Mul(Signature s, SecretKey k)
        {
            if (s == null)
            {
                return new Signature(new G1().ScalarBaseMult(k));
            }

            return new Signature(new G1().ScalarMult(s.Sig, k));
        }
    }
}
