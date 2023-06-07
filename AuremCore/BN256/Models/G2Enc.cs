using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.BN256.Models
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct G2Enc
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 129)]
        public byte[] bytes;

        public G2Enc()
        {
            bytes = new byte[129];
        }
    }
}
