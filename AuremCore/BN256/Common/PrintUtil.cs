using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.BN256.Common
{
    internal static class PrintUtil
    {
        public static string Hexify(byte[] bytes)
        {
            if (bytes == null) return "";

            StringBuilder rv = new(bytes.Length * 2);

            for (int i = 0; i < bytes.Length; i++)
            {
                rv.Append("0123456789abcdef"[bytes[i] >> 4]);
                rv.Append("0123456789abcdef"[bytes[i] & 0xf]);
            }

            return rv.ToString();
        }
    }
}
