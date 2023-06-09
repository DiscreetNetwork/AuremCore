using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Crypto.BN256.Models
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct GFp6
    {
        [MarshalAs(UnmanagedType.Struct)]
        public GFp2 x;

        [MarshalAs(UnmanagedType.Struct)]
        public GFp2 y;

        [MarshalAs(UnmanagedType.Struct)]
        public GFp2 z;

        public GFp6()
        {
            x = new GFp2();
            y = new GFp2();
            z = new GFp2();
        }

        public void Set(GFp6 b)
        {
            x.Set(b.x);
            y.Set(b.y);
            z.Set(b.z);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == null) return false;

            if (obj is GFp6 b)
            {
                return x.Equals(b.x) && y.Equals(b.y) && z.Equals(b.z);
            }

            return false;
        }

        public override string ToString()
        {
            return $"{{{x}, {y}, {z}}}";
        }
    }
}
