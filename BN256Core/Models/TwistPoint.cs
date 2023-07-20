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
    public struct TwistPoint
    {
        [MarshalAs(UnmanagedType.Struct)]
        public GFp2 x;

        [MarshalAs(UnmanagedType.Struct)]
        public GFp2 y;

        [MarshalAs(UnmanagedType.Struct)]
        public GFp2 z;

        [MarshalAs(UnmanagedType.Struct)]
        public GFp2 t;

        public TwistPoint()
        {
            x = new GFp2();
            y = new GFp2();
            z = new GFp2();
            t = new GFp2();
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == null) return false;

            if (obj is TwistPoint b)
            {
                return x.Equals(b.x) && y.Equals(b.y) && z.Equals(b.z) && t.Equals(b.t);
            }

            return false;
        }

        public void Set(TwistPoint a)
        {
            x.Set(a.x);
            y.Set(a.y);
            z.Set(a.z);
            t.Set(a.t);
        }

        public override string ToString()
        {
            return $"{{\n\t{PrintUtil.Hexify(Util.FpToBytes(x.x), true)},\n\t{PrintUtil.Hexify(Util.FpToBytes(x.y), true)}\n}},\n{{\n\t{PrintUtil.Hexify(Util.FpToBytes(y.x), true)},\n\t{PrintUtil.Hexify(Util.FpToBytes(y.y), true)}\n}},\n{{\n\t{PrintUtil.Hexify(Util.FpToBytes(z.x), true)},\n\t{PrintUtil.Hexify(Util.FpToBytes(z.y), true)}\n}},\n{{\n\t{PrintUtil.Hexify(Util.FpToBytes(t.x), true)},\n\t{PrintUtil.Hexify(Util.FpToBytes(t.y), true)}\n}},\n";
        }
    }
}
