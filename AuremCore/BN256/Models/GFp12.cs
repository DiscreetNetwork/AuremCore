using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.BN256.Models
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct GFp12
    {
        [MarshalAs(UnmanagedType.Struct)]
        public GFp6 x;

        [MarshalAs(UnmanagedType.Struct)]
        public GFp6 y;
    }
}
