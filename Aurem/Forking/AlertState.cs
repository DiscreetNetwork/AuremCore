using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Forking
{
    public enum AlertState : byte
    {
        Alert = 0,
        Proving = 1,
        Finished = 2,
        Request = 3
    }
}
