using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.RMC
{
    public enum Status : byte
    {
        Unknown = 0,
        Data = 1,
        Signed = 2,
        Finished = 3,
    }
}
