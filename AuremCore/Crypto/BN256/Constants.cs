using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Crypto.BN256
{
    public static class Constants
    {                                                           //  65000549695646603732796438742359905742570406053903786389881062969044166799969
        public static readonly BigInteger Order = BigInteger.Parse("65000549695646603732796438742359905742570406053903786389881062969044166799969");

        public static readonly int SignatureLength = 64;

        internal static readonly byte[] DST = new byte[] { (byte)'a', (byte)'u', (byte)'r', (byte)'e', (byte)'m', (byte)'-', (byte)'s', (byte)'i', (byte)'g' };

        public static readonly Models.G1 G1Gen;
        public static readonly Models.G2 G2Gen;

        static Constants()
        {
            G1Gen = new Models.G1().ScalarBaseMult(new SecretKey(new BigInteger(1)));
            G2Gen = new Models.G2().ScalarBaseMult(new SecretKey(new BigInteger(1)));
        }

    }
}
