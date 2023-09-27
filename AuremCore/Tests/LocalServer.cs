using AuremCore.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace AuremCore.Tests
{
    public class LocalServer : Server
    {
        private ChannelWriter<Conn>[] dialChans;
        private Channel<Conn> listenChan;
        private TimeSpan timeout;

        public LocalServer(Channel<Conn>[] chans, int id, TimeSpan timeout)
        {
            this.timeout = timeout;
            dialChans = chans.Select(x => x.Writer).ToArray();
            listenChan = chans[id];
        }

        public static (Conn, Conn) NewConn()
        {
            var c1 = Channel.CreateUnbounded<byte[]>();
            var c2 = Channel.CreateUnbounded<byte[]>();

            return (new LocalConn(c2.Writer, c1.Reader), new LocalConn(c1.Writer, c2.Reader));
        }

        public static Server[] NewNetwork(int len, TimeSpan timeout)
        {
            var chans = new Channel<Conn>[len];
            for (int i = 0; i < len; i++)
            {
                chans[i] = Channel.CreateBounded<Conn>(1);
            }

            var servers = new LocalServer[len];
            for (int i = 0; i < len; i++)
            {
                servers[i] = new LocalServer(chans, i, timeout);
            }

            return servers;
        }

        public static void CloseNetwork(Server[] servers)
        {
            foreach (var ns in servers)
            {
                if (ns is LocalServer serv)
                {
                    serv.listenChan.Writer.Complete();
                }
            }
        }
        
        public override void Stop()
        {
            // do nothing
        }

        public override async Task<Conn?> Dial(ushort pid)
        {
            (var _out, var _in) = NewConn();
            if (pid >= dialChans.Length)
            {
                throw new Exception("unknown host");
            }

            CancellationTokenSource ctsTimeout = new CancellationTokenSource();
            ctsTimeout.CancelAfter(timeout);
            await dialChans[pid].WriteAsync(_in, ctsTimeout.Token);
            if (ctsTimeout.IsCancellationRequested)
            {
                throw new Exception("Dial timeout");
            }

            return _out;
        }

        public override async Task<Conn> Listen()
        {
            CancellationTokenSource ctsTimeout = new CancellationTokenSource();
            ctsTimeout.CancelAfter(timeout);

            if (listenChan.Reader.Completion.IsCompleted)
            {
                throw new Exception("done");
            }

            var conn = await listenChan.Reader.ReadAsync(ctsTimeout.Token);
            if (ctsTimeout.IsCancellationRequested)
            {
                throw new Exception("Listen timeout");
            }

            return conn;
        }
    }
}
