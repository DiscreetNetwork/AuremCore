using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Crypto.BN256.Models
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct BN
    {
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U4, SizeConst = 16)]
        public uint[] array;

        public BN()
        {
            array = new uint[16];
        }

        public static BigInteger FromBN(BN a)
        {
            byte[] data = new byte[65];
            for (int i = 0; i < a.array.Length; i++)
            {
                data[4 * i] = (byte)(a.array[i] & 0xff);
                data[4 * i + 1] = (byte)(a.array[i] >> 8 & 0xff);
                data[4 * i + 2] = (byte)(a.array[i] >> 16 & 0xff);
                data[4 * i + 3] = (byte)(a.array[i] >> 24 & 0xff);
            }
            data[64] = 0;
            return new BigInteger(data);
        }

        public static void ToBN(BigInteger c, uint[] array)
        {
            if (array.Length != 16) throw new Exception("invalid use");

            var d = c.ToByteArray();
            int i;
            for (i = 0; i < d.Length / 4; i++)
            {
                array[i] = d[4 * i];
                array[i] += (uint)d[4 * i + 1] << 8;
                array[i] += (uint)d[4 * i + 1] << 16;
                array[i] += (uint)d[4 * i + 1] << 24;
            }

            if (d.Length % 4 != 0)
            {
                for (int shift = 0; 4 * i + shift < d.Length; shift++)
                {
                    array[i] = (uint)d[4 * i + shift] << 8 * shift;
                }
            }

            Array.Clear(d);
        }

        public BN(BigInteger c) : this()
        {
            ToBN(c, array);
        }
    }
}
