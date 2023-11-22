using Aurem.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Syncing
{
    public static class Requests
    {
        public delegate Task Gossip(ushort pid);
        public delegate Task Fetch(ushort pid, ulong[] unitIDs);
        public delegate Task Multicast(IUnit u);
    }
}
