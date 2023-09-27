using AuremCore.Network;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace AuremCore.Tests
{
    public class LocalConn : Conn
    {
        //private sealed class QueueStream : Stream
        //{
        //    private readonly ConcurrentQueue<byte[]> s_Queue;
        //    private readonly TimeSpan s_Timeout;
        //    private long s_Position;

        //    public override bool CanRead => true;

        //    public override bool CanSeek => false;

        //    public override bool CanWrite => true;

        //    public override long Position => s_Position;

        //    public QueueStream(TimeSpan timeout)
        //    {
        //        s_Position = 0;
        //        s_Timeout = timeout;
        //        s_Queue = new ConcurrentQueue<byte[]>();
        //    }

        //    public override int Write(byte[] buffer, int offset, int count)
        //    {
        //        var bytes = new byte[count];
        //        Buffer.BlockCopy(buffer, offset, bytes, 0, count);
        //        s_Queue.Enqueue(bytes);
        //        s_Position += count;
        //    }

        //    public override void Flush()
        //    {
        //        // do nothing
        //    }

        //    public override 
        //}
        private ChannelReader<byte[]> read;
        private ChannelWriter<byte[]> write;

        private readonly SemaphoreSlim readMutex;
        private byte[]? _leftover;
        
        private CancellationTokenSource cancellationTokenSource;

        public override bool IsConnected => !cancellationTokenSource.IsCancellationRequested;

        public LocalConn(ChannelWriter<byte[]> write, ChannelReader<byte[]> read)
        {
            readMutex = new SemaphoreSlim(1, 1);
            cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Token.Register(() => write.Complete());
            this.write = write;
            this.read = read;
        }

        public override Task Close()
        {
            cancellationTokenSource.Cancel();

            return Task.CompletedTask;
        }

        public override void Flush()
        {
            // do nothing
        }

        public override Task Interrupt()
        {
            return Task.CompletedTask;
        }

        public override async Task<int> Read(byte[] s)
        {
            var offset = 0;

            await readMutex.WaitAsync();

            try
            {
                if (_leftover is not null)
                {
                    if (_leftover.Length >= s.Length)
                    {
                        Buffer.BlockCopy(_leftover, 0, s, 0, s.Length);
                        _leftover = _leftover[s.Length..];
                        if (_leftover.Length == 0) _leftover = null;
                        return s.Length;
                    }
                    else
                    {
                        Buffer.BlockCopy(_leftover, 0, s, 0, _leftover.Length);
                        offset = _leftover.Length;

                        _leftover = null;
                    }
                }

                while (offset < s.Length)
                {
                    var bytes = await read.ReadAsync(cancellationTokenSource.Token);
                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        return offset;
                    }

                    var rem = s.Length - offset;
                    if (bytes.Length >= rem)
                    {
                        Buffer.BlockCopy(bytes, 0, s, offset, rem);
                        _leftover = bytes[rem..];
                        if (_leftover.Length == 0) _leftover = null;
                        return s.Length;
                    }
                    else
                    {
                        Buffer.BlockCopy(bytes, 0, s, offset, bytes.Length);
                        offset += bytes.Length;
                    }
                }

                return offset;
            }
            finally
            {
                readMutex.Release();
            }
        }

        public override async Task<int> Write(byte[] data)
        {
            if (cancellationTokenSource.IsCancellationRequested) return 0;

            await write.WriteAsync(data);
            return data.Length;
        }

        public override IPEndPoint RemoteEndPoint => null!;
    }
}
