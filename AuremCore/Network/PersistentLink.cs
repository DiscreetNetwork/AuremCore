using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Network
{
    /// <summary>
    /// Wraps a persistent TCP connection and distributes incoming traffic to multiple virtual connections (<see cref="Conn"/>).
    /// It comes in two variants: outgoing link (allows creating new conns with Call()) and incoming link (creates a new con upon receiving data with unknown ID and puts that conn in the listener queue).
    /// The two variants are distinguished by the existence of said queue; outgoing links have null, versus incoming having the queue instantiated.
    /// When encountering an error during reading, the link shuts down the TCP connection and marks itself as dead. 
    /// To restore communication, a new link must be created.
    /// </summary>
    public class PersistentLink
    {
        private TcpClient TcpLink;
        private IPEndPoint RemoteAddress;
        private ConcurrentDictionary<ulong, PersistentConn> Connections;
        private ConcurrentQueue<Conn> Queue;
        private ulong LastId;
        private ulong Quit;

        private ulong WaitGroup;
        private SemaphoreSlim Mutex;

        public LinkedList(TcpClient tcpLink, ConcurrentQueue<Conn> queue, ref ulong waitGroup, ref ulong quit)
        {

        }
    }
}
