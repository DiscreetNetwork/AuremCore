using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.BN256.Models
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct G2
    {
        [MarshalAs(UnmanagedType.Struct)]
        public TwistPoint p;
    }
}
