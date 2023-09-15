using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BN256Core.Extensions
{
    public static class ByteArrayExtensions
    {
        public static int Compare(this byte[] a, byte[] b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;

            if (a.Length > b.Length) return 1;
            if (a.Length < b.Length) return -1;

            int i;

            for (i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    break;
                }
            }

            if (i == a.Length)
            {
                return 0;
            }
            else if (a[i] > b[i])
            {
                return 1;
            }
            else
            {
                return -1;
            }
        }

        public static bool BEquals(this byte[] a, byte[] b) => Compare(a, b) == 0;

        public static byte[] Concat(this byte[] a, ReadOnlySpan<byte> b)
        {
            if (a == null && b == null) return Array.Empty<byte>();

            if (a == null) return b.ToArray();
            if (b == null) return a;

            byte[] r = new byte[a.Length + b.Length];

            Buffer.BlockCopy(r, 0, a, 0, a.Length);
            b.CopyTo(r.AsSpan(a.Length));

            return r;
        }

        public static byte[] Concat(this byte[] a, byte[] b)
        {
            if (a == null && b == null) return Array.Empty<byte>();

            if (a == null) return b.ToArray();
            if (b == null) return a;

            byte[] r = new byte[a.Length + b.Length];

            Buffer.BlockCopy(r, 0, a, 0, a.Length);
            Buffer.BlockCopy(r, a.Length, b, 0, b.Length);

            return r;
        }

        public static byte[] Concat(this byte[] a, params byte[][] arrs)
        {
            if (a == null && arrs?.Length == null) return Array.Empty<byte>();
            if (arrs == null) return a ?? Array.Empty<byte>();

            var iter = arrs.Append(a).Where(x => x != null);
            byte[] rv = new byte[arrs.Sum(x => x.Length)];
            var offset = 0;

            foreach (var b in iter)
            {
                Buffer.BlockCopy(rv, offset, b, 0, b.Length);
                offset += b.Length;
            }

            return rv;
        }
    }
}
