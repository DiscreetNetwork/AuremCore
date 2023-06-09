using AuremCore.Crypto.BN256.Common;
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
    internal struct GFp2
    {
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U8, SizeConst = 4)]
        public ulong[] x;
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U8, SizeConst = 4)]
        public ulong[] y;

        public GFp2()
        {
            x = new ulong[4];
            y = new ulong[4];
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == null) return false;

            if (obj is GFp2 b)
            {
                return Util.GFpEqual(y, b.y) && Util.GFpEqual(x, b.x);
            }

            return false;
        }

        public void Set(GFp2 a)
        {
            Array.Copy(a.x, x, 4);
            Array.Copy(a.y, y, 4);
        }

        public override string ToString()
        {
            return "{" + PrintUtil.Hexify(Util.FpToBytes(x), true) + ", " + PrintUtil.Hexify(Util.FpToBytes(y), true) + "}";
        }
    }
}
