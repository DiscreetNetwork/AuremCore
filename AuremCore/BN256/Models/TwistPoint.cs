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
    }
}
