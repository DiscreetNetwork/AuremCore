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

            if (bytes.Length % 8 != 0)
            {
                StringBuilder rv = new(bytes.Length * 2);

                for (int i = 0; i < bytes.Length; i++)
                {
                    rv.Append("0123456789abcdef"[bytes[i] >> 4]);
                    rv.Append("0123456789abcdef"[bytes[i] & 0xf]);
                }

                return rv.ToString();
            }
            else
            {
                StringBuilder rv = new();
                rv.Append("{");
                for (int i = 0; i < bytes.Length / 8; i++)
                {
                    rv.Append("0x");
                    for (int j = 7; j >= 0; j--)
                    {
                        rv.Append("0123456789abcdef"[bytes[8 * i + j] >> 4]);
                        rv.Append("0123456789abcdef"[bytes[8 * i + j] & 0xf]);
                    }

                    if (i < bytes.Length/8 - 1)
                    {
                        rv.Append(", ");
                    }
                }
                rv.Append("}");

                return rv.ToString();
            }
        }
    }
}
