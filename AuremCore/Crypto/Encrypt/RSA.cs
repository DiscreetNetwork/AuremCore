using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Crypto.Encrypt
{
    public static class RSA
    {
        public static (EncryptionKey, DecryptionKey) GenerateKeys()
        {
            using RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048);
            var p = rsa.ExportParameters(true);
            return (new EncryptionKey { e = (byte[])p.Exponent.Clone(), n = (byte[])p.Modulus.Clone() }, new DecryptionKey { e = p.Exponent, d = p.D, p = p.P, q = p.Q, n = p.Modulus, dp = p.DP, dq = p.DQ, iq = p.InverseQ });
        }
    }
}
