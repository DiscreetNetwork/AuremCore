using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.BN256.Models
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct GFp6
    {
        [MarshalAs(UnmanagedType.Struct)]
        public GFp2 x;
        
        [MarshalAs(UnmanagedType.Struct)]
        public GFp2 y;

        [MarshalAs(UnmanagedType.Struct)]
        public GFp2 z;

        public GFp6()
        {
            x = new GFp2();
            y = new GFp2();
            z = new GFp2();
        }

        public override string ToString()
        {
            return $"{{{x}, {y}, {z}}}";
        }
    }
}
