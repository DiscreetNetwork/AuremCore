using BN256Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BN256Core.Models
{
    [StructLayout(LayoutKind.Sequential)]
    public struct G1Enc
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] bytes;

        public G1Enc()
        {
            bytes = new byte[64];
        }

        public override string ToString()
        {
            return $"{PrintUtil.Hexify(bytes)}";
        }
    }
}
