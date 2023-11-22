using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Network
{
    /// <summary>
    /// Implements "virtual connections", many of which utilize the same underlying TCP link.
    /// Each virtual connection has a unique ID and every piece of data sent through the common TCP link is prefixed with a 12-byte header consisting of the ID and length of data.
    /// ID is 8 bytes, and the length is 4 bytes.
    /// All writes are buffered and the actual network traffic happens only on Flush() (explicit) or when the buffer is full.
    /// Reads are also buffered and they read bytes from the stream populated by the link supervising the connection.
    /// Close() sends a header with data length zero. After closing the connection, calling Write() or Flush() throws an error, but reading is still possible until the underlying stream is depleted.
    /// 
    /// Note: Write() and Flush() might not be thread safe.
    /// </summary>
    public class PersistentConn : Conn
    {
        public sealed class ChanReader
        {
            public Queue<byte[]> Buf;
            public TaskCompletionSource<bool> Closed;

            //...
        }

        public static readonly int HeaderSize = 12;
        public static readonly int BufferSize = (1 << 14) - HeaderSize;

        public static (ulong, uint) ParseHeader(ReadOnlySpan<byte> buffer) => (BinaryPrimitives.ReadUInt64LittleEndian(buffer), BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(8)));

        public ulong Id;
        public ulong Closing;

        private PersistentLink Link;
        private byte[] Frame;
        private byte[] Buffer;
        private int Sent;
        private int Received;
        private Queue<byte[]> Que;
        private TaskCompletionSource<bool> Closed;

        public PersistentConn(ulong id, PersistentLink link)
        {
            Frame = new byte[HeaderSize + BufferSize];
            BinaryPrimitives.WriteUInt64LittleEndian(Frame, id);
            Que = new(32);

            Id = id;
            Link = link;
            Sent = 0;
            Received = 0;
            Closed = new TaskCompletionSource<bool>();
        }

        //public override Stream NetStream => null;

        public bool IsClosed()
        {
            return Closed.Task.IsCompleted;
        }

        public override async Task Close()
        {
            if (IsClosed()) return;
            
            if (Interlocked.CompareExchange(ref Closing, 1, 0) == 0)
            {
                await SendFinished();
                ConnFinalize();

            }
        }

        public void Erase()
        {
            Link.EraseConn(Id);
        }

        public void ConnFinalize()
        {
            Closed.SetResult(true);
        }

        public async Task SendFinished()
        {
            var header = new byte[HeaderSize];
            BinaryPrimitives.WriteUInt64LittleEndian(header, Id);
            BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(8), 0);
            await Link.TcpLink.GetStream().WriteAsync(header);
        }

        public void Enqueue(byte[] data)
        {
            Que.Enqueue(data);
        }

        public override IPEndPoint RemoteEndPoint { get => Link.RemoteAddr(); }
        public override bool IsConnected => Link.TcpLink.Connected;

        public override async Task Interrupt()
        {
            // null task
            return;
        }

        public void TimeoutAfter(TimeSpan timeout)
        {
            if (!Link.IsDead())
            {
                Link.TcpLink.SendTimeout = (int)Math.Floor(timeout.TotalMilliseconds);
            }

            _ = Task.Run(async () => {
                await Task.Delay(timeout);
                await Close();
            });
        }

        public async Task LocalClose()
        {
            if (Interlocked.CompareExchange(ref Closing, 1, 0) == 0)
            {
                ConnFinalize();
                Erase();
            }
        }

        public override void Flush()
        {
            // DEPRECATED.
            return;
        }

        public override async Task<int> Write(byte[] data)
        {
            if (IsClosed()) return 0;

            // DEPRECATED.
            return 0;
        }

        public override async Task<int> Read(byte[] data)
        {
            // DEPRECATED.

            return 0;
        }
    }
}
