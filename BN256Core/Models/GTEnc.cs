using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BN256Core.Models
{
    [StructLayout(LayoutKind.Sequential)]
    public struct GTEnc
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 384)]
        public byte[] bytes;

        public GTEnc()
        {
            bytes = new byte[384];
        }
    }
}
