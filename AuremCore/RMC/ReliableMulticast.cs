using AuremCore.Crypto.Multi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.RMC
{
    public class ReliableMulticast
    {
        private SemaphoreSlim _inMx = new SemaphoreSlim(1, 1);
        private SemaphoreSlim _outMx = new SemaphoreSlim(1, 1);

        private Keychain keys;
        //private Dictionary
    }
}
