﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Syncing.Internals.Packets
{
    public enum PacketID : byte
    {
        NONE = 0,
        
        // gossip packets
        GOSSIPGREET = 1,
        GOSSIPINFO = 2,
        GOSSIPUNITS = 3,

        // multicast
        MCASTSEND = 16,

        // fetch
        FETCHREQUEST = 32,
        FETCHRESPONSE = 33,

        // Rmc
        RMCSignature = 48,
        RMCSendData = 49,
        RMCRequestFinished = 50,
        RMCFinished = 51,
        RMCProof = 52,
        RMCBroadcastSignature = 53,
        RMCBroadcastMultisignature = 54
    }
}
