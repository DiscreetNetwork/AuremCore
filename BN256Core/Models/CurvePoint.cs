using BN256Core.Common;
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
    public struct CurvePoint
    {
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U8, SizeConst = 4)]
        public ulong[] x;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U8, SizeConst = 4)]
        public ulong[] y;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U8, SizeConst = 4)]
        public ulong[] z;

        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U8, SizeConst = 4)]
        public ulong[] t;

        public CurvePoint()
        {
            x = new ulong[4];
            y = new ulong[4];
            z = new ulong[4];
            t = new ulong[4];
        }

        public override string ToString()
        {
            return $"{PrintUtil.Hexify(Util.FpToBytes(x), true)}, {PrintUtil.Hexify(Util.FpToBytes(y), true)}, {PrintUtil.Hexify(Util.FpToBytes(z), true)}, {PrintUtil.Hexify(Util.FpToBytes(t), true)}";
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == null) return false;

            if (obj is CurvePoint b)
            {
                return Util.GFpEqual(y, b.y) && Util.GFpEqual(x, b.x) && Util.GFpEqual(z, b.z) && Util.GFpEqual(t, b.t);
            }

            return false;
        }

        public CurvePoint Set(CurvePoint a)
        {
            Array.Copy(a.x, x, 4);
            Array.Copy(a.y, y, 4);
            Array.Copy(a.z, z, 4);
            Array.Copy(a.t, t, 4);
            return this;
        }
    }
}
