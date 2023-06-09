using AuremCore.Crypto.BN256.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Crypto.BN256.Models
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct G1Enc
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
