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

        public override string ToString()
        {
            return "{" + PrintUtil.Hexify(Util.FpToBytes(x), true) + ", " + PrintUtil.Hexify(Util.FpToBytes(y), true) + "}";
        }
    }
}
