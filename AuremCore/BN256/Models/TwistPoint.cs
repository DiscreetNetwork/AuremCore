using AuremCore.BN256.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.BN256.Models
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct TwistPoint
    {
        [MarshalAs(UnmanagedType.Struct)]
        public GFp2 x;

        [MarshalAs(UnmanagedType.Struct)]
        public GFp2 y;

        [MarshalAs(UnmanagedType.Struct)]
        public GFp2 z;

        [MarshalAs (UnmanagedType.Struct)]
        public GFp2 t;

        public TwistPoint()
        {
            x = new GFp2();
            y = new GFp2();
            z = new GFp2();
            t = new GFp2();
        }

        public override string ToString()
        {
            return $"{{\n\t{PrintUtil.Hexify(Util.FpToBytes(x.x), true)},\n\t{PrintUtil.Hexify(Util.FpToBytes(x.y), true)}\n}},\n{{\n\t{PrintUtil.Hexify(Util.FpToBytes(y.x), true)},\n\t{PrintUtil.Hexify(Util.FpToBytes(y.y), true)}\n}},\n{{\n\t{PrintUtil.Hexify(Util.FpToBytes(z.x), true)},\n\t{PrintUtil.Hexify(Util.FpToBytes(z.y), true)}\n}},\n{{\n\t{PrintUtil.Hexify(Util.FpToBytes(t.x), true)},\n\t{PrintUtil.Hexify(Util.FpToBytes(t.y), true)}\n}},\n";
        }
    }
}
