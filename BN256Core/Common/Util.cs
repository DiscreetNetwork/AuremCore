using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BN256Core.Common
{
    public static class Util
    {
        public static bool GFpEqual([NotNullWhen(true)] ulong[] a, [NotNullWhen(true)] ulong[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }

            return true;
        }

        public static byte[] FpToBytes(ulong[] x)
        {
            byte[] bytes = new byte[8 * x.Length];
            for (int i = 0; i < x.Length; i++)
            {
                bytes[8 * i + 0] = (byte)(x[i] >> 56);
                bytes[8 * i + 1] = (byte)(x[i] >> 48 & 0xff);
                bytes[8 * i + 2] = (byte)(x[i] >> 40 & 0xff);
                bytes[8 * i + 3] = (byte)(x[i] >> 32 & 0xff);
                bytes[8 * i + 4] = (byte)(x[i] >> 24 & 0xff);
                bytes[8 * i + 5] = (byte)(x[i] >> 16 & 0xff);
                bytes[8 * i + 6] = (byte)(x[i] >> 8 & 0xff);
                bytes[8 * i + 7] = (byte)(x[i] & 0xff);
            }

            return bytes;
        }
    }
}
