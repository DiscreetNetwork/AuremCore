using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Syncing
{
    public class RmcRequest
    {
        public byte MsgType;
        public ulong Id;
        public byte[] Data;

        public RmcRequest(ulong id, byte[] data, byte msgType)
        {
            Id = id;
            Data = data;
            MsgType = msgType;
        }
    }
}
