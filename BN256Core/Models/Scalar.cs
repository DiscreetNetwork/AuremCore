﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BN256Core.Models
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Scalar
    {
        [MarshalAs(UnmanagedType.Struct)]
        public BN n;

        public Scalar()
        {
            n = new BN();
        }

        public Scalar(BigInteger value)
        {
            n = new BN(value);
        }
    }
}
