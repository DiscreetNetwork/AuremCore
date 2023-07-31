using BN256Core;
using BN256Core.Extensions;
using BN256Core.Models;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Crypto.P2P
{
    public class P2PPublicKey
    {
        public G1 g1;
        public G2 g2;

        public P2PPublicKey() { }

        public P2PPublicKey(G1 g1, G2 g2)
        {
            this.g1 = g1;
            this.g2 = g2;
        }

        public P2PPublicKey(SecretKey k)
        {
            g1 = new G1().ScalarBaseMult(k);
            g2 = new G2().ScalarBaseMult(k);
        }

        public bool Verify()
        {
            var p1 = new GT().Pair(g1, P2P.GenG2).Marshal();
            var p2 = new GT().Pair(P2P.GenG1, g2).Marshal();

            return p1.BEquals(p2);
        }

        public byte[] Marshal()
        {
            var g1m = g1.Marshal();
            var g2m = g2.Marshal();

            byte[] res = new byte[4+g1m.Length+g2m.Length];

            BinaryPrimitives.WriteInt32LittleEndian(res.AsSpan(0, 4), g1m.Length);
            Array.Copy(g1m, 0, res, 4, g1m.Length);
            Array.Copy(g2m, 0, res, 4 + g1m.Length, g2m.Length);

            return res;
        }

        public P2PPublicKey Unmarshal(byte[] data)
        {
            if (data.Length < 4) throw new Exception("data too short");

            var g1len = BinaryPrimitives.ReadInt32LittleEndian(data);
            if (data.Length < g1len + 4) throw new Exception("data too short");

            g1 = new G1().Unmarshal(data.AsSpan(4));
            g2 = new G2().Unmarshal(data.AsSpan(4 + g1len));

            return this;
        }

        public string Encode()
        {
            return Convert.ToBase64String(Marshal());
        }

        public static P2PPublicKey Decode(string enc)
        {
            return new P2PPublicKey().Unmarshal(Convert.FromBase64String(enc));
        }
    }
}
