using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace AuremCore.Network
{
    
    public class NetReader
    {
        private Stream s;
        private bool _closed;
        private ConcurrentQueue<byte[]> Ch;

        public NetReader(Stream s)
        {

        }
    }
}
