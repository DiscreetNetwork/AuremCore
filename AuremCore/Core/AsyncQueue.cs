using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Channels;

namespace AuremCore.Core
{
    public class AsyncQueue<T>
    {
        private Channel<T> ch;

        public AsyncQueue()
        {
            ch = Channel.CreateUnbounded<T>();
        }

        public ValueTask Enqueue(T item)
        {
            return ch.Writer.WriteAsync(item);
        }

        public ValueTask AddCallbackOnReceive(Action a)
        {
            ch.Reader.
        }
    }
}
