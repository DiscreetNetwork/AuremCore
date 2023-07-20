using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BN256Core.Models
{
    [StructLayout(LayoutKind.Sequential)]
    public struct GFp12
    {
        [MarshalAs(UnmanagedType.Struct)]
        public GFp6 x;

        [MarshalAs(UnmanagedType.Struct)]
        public GFp6 y;

        public GFp12()
        {
            x = new GFp6();
            y = new GFp6();
        }

        public void Set(GFp12 b)
        {
            x.Set(b.x);
            y.Set(b.y);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == null) return false;

            if (obj is GFp12 b)
            {
                return x.Equals(b.x) && y.Equals(b.y);
            }

            return false;
        }

        public override string ToString()
        {
            return $"{{{x}, {y}}}";
        }
    }
}
