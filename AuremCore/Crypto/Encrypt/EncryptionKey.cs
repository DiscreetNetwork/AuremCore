using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Crypto.Encrypt
{
    public class EncryptionKey
    {
        public byte[] e;
        public byte[] n;

        public byte[] Encrypt(byte[] msg)
        {
            var rp = new RSAParameters { Exponent = e, Modulus = n };
            var rsa = System.Security.Cryptography.RSA.Create(rp);
            return rsa.Encrypt(msg, RSAEncryptionPadding.OaepSHA256);
        }

        public string Encode()
        {
            var se = Convert.ToBase64String(e);
            var sn = Convert.ToBase64String(n);
            return $"{sn}#{se}";
        }

        public static EncryptionKey Decode(string key)
        {
            var ss = key.Split('#');
            var n = Convert.FromBase64String(ss[0]);
            var e = Convert.FromBase64String(ss[1]);

            return new EncryptionKey { e = e, n = n };
        }
    }
}
