using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Syncing
{
    public class BitSet
    {
        private uint[] arr;

        public BitSet(int n)
        {
            if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
            arr = new uint[(n + 31) >> 5];
        }

        public BitSet(byte[] array)
        {
            if (array == null)
            {
                arr = Array.Empty<uint>();
                return;
            }

            arr = new uint[(array.Length + 3) / 4];
            for (int i = 0; i < array.Length / 4; i++)
            {
                arr[i] = array[4 * i];
                arr[i] |= ((uint)array[4 * i + 1] << 8);
                arr[i] |= ((uint)array[4 * i + 2] << 16);
                arr[i] |= ((uint)array[4 * i + 3] << 24);
            }

            var off = (array.Length >> 2) << 2;
            while (off < arr.Length)
            {
                arr[off / 4] |= (uint)array[off] << ((off % 4) * 8);
                off++;
            }
        }

        public void Set(int k)
        {
            if (k < 0 || k >= 32*arr.Length) throw new ArgumentOutOfRangeException(nameof(k));
            arr[k >> 5] |= (uint)(1 << (k & 31));
        }

        public bool Test(int k)
        {
            if (k < 0 || k >= 32 * arr.Length) throw new ArgumentOutOfRangeException(nameof(k));
            return (arr[k >> 5] & (1 << (k & 31))) != 0;
        }
    }
}
