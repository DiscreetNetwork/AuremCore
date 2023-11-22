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
    public static class P2P
    {
        public static readonly G1 GenG1 = new G1().ScalarBaseMult(new SecretKey(1));
        public static readonly G2 GenG2 = new G2().ScalarBaseMult(new SecretKey(1));
    
        public static (P2PPublicKey, P2PSecretKey) GenerateKeys()
        {
            SecretKey sk = new SecretKey(SecretKey.RandomScalar());
            return (new P2PPublicKey(sk), new P2PSecretKey(sk));
        }

        public static SymmetricKey[] Keys(P2PSecretKey sk, P2PPublicKey[] pks, ushort pid)
        {
            var nproc = pks.Length;
            var res = new SymmetricKey[nproc];
            for (ushort i = 0; i < nproc; i++)
            {
                res[i] = new SharedSecret(sk, pks[i]).Key();
            }

            return res;
        }
    }
}
