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
    internal struct G2Enc
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 129)]
        public byte[] bytes;

        public G2Enc()
        {
            bytes = new byte[129];
        }

        public override string ToString()
        {
            var bs = new byte[128];
            Array.Copy(bytes, 1, bs, 0, 128);
            return $"{bytes[0]}: {PrintUtil.Hexify(bs)}";
        }
    }
}
