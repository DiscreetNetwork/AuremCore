using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Crypto.Encrypt
{
    public class DecryptionKey
    {
        public byte[] d;
        public byte[] p;
        public byte[] q;
        public byte[] dp;
        public byte[] dq;
        public byte[] e;
        public byte[] n;
        public byte[] iq;

        public byte[] Decrypt(byte[] msg)
        {
            var rp = new RSAParameters { D = d, P = p, Q = q, Modulus = n, DP = dp, DQ = dq, InverseQ = iq, Exponent = e };
            var rsa = System.Security.Cryptography.RSA.Create(rp);
            return rsa.Decrypt(msg, RSAEncryptionPadding.OaepSHA256);
        }

        public string Encode()
        {
            var rp = new RSAParameters { D = d, P = p, Q = q, Modulus = n, DP = dp, DQ = dq, InverseQ = iq, Exponent = e };
            var rsa = System.Security.Cryptography.RSA.Create(rp);
            return Convert.ToBase64String(rsa.ExportPkcs8PrivateKey());
        }

        public static DecryptionKey Decode(string key)
        {
            var pk = Convert.FromBase64String(key);
            var rsa = System.Security.Cryptography.RSA.Create();
            rsa.ImportPkcs8PrivateKey(pk, out _);
            var p = rsa.ExportParameters(true);
            return new DecryptionKey { e = p.Exponent, d = p.D, p = p.P, q = p.Q, n = p.Modulus, dp = p.DP, dq = p.DQ, iq = p.InverseQ };
        }
    }
}
