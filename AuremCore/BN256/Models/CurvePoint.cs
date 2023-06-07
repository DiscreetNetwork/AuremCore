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
    internal struct CurvePoint
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
            return $"{PrintUtil.Hexify(Util.FpToBytes(x))}, {PrintUtil.Hexify(Util.FpToBytes(y))}, {PrintUtil.Hexify(Util.FpToBytes(z))}, {PrintUtil.Hexify(Util.FpToBytes(t))}";
        }
    }
}
