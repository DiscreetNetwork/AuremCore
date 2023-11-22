using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Core
{
    public class ValueSignal<T>
    {
        public T Value
        {
            get
            {
                return Wait();
            }

            set
            {
                Send(value);
            }
        }

        public Task<T> ReceiveTask
        {
            get
            {
                return WaitAsync();
            }
        }

        public bool IsClosed
        {
            get
            {
                return Interlocked.Read(ref _closed) != 0;
            }
        }

        public bool HasValue
        {
            get
            {
                return _out != null && _out.CurrentCount > 0;
            }
        }

        public bool CanReceive
        {
            get
            {
                return _in != null && _in.CurrentCount > 0;
            }
        }

        private SemaphoreSlim _in;
        private SemaphoreSlim _out;
        private T _val;
        private ulong _closed;
        private readonly object _closerLock = new object();

#pragma warning disable CS8618
        public ValueSignal()
#pragma warning restore CS8618
        {
            _in = new SemaphoreSlim(1, 1);
            _out = new SemaphoreSlim(0, 1);
            _closed = 0;
        }

        public ValueSignal(T initial)
        {
            _in = new SemaphoreSlim(0, 1);
            _out = new SemaphoreSlim(1, 1);
            _val = initial;
            _closed = 0;
        }

        public void Send(T value)
        {
            if (Interlocked.Read(ref _closed) != 0)
            {
                return;
            }

            _in.Wait();

            if (Interlocked.Read(ref _closed) != 0)
            {
                return;
            }

            _val = value;
            _out?.Release();
        }

        public T Wait()
        {
#pragma warning disable CS8603 // Possible null reference return.
            if (Interlocked.Read(ref _closed) != 0) return default;
#pragma warning restore CS8603 // Possible null reference return.

            _out.Wait();
            var rv = _val;

            if (Interlocked.Read(ref _closed) == 0) _in.Release();
            else
            {
                lock (_closerLock)
                {
                    _val = default!;
                    _out.Dispose();
                    _out = null!;
                }
            }

            return rv;
        }

        public async Task<T> WaitAsync()
        {
#pragma warning disable CS8603 // Possible null reference return.
            if (Interlocked.Read(ref _closed) != 0) return default;
#pragma warning restore CS8603 // Possible null reference return.


            await _out.WaitAsync();
            var rv = _val;
            if (Interlocked.Read(ref _closed) == 0) _in.Release();
            else
            {
                lock (_closerLock)
                {
                    _val = default!;
                    _out.Dispose();
                    _out = null!;
                }
            }

            return rv;
        }

        public async Task SendAsync(T value)
        {
            if (Interlocked.Read(ref _closed) != 0) return;

            await _in.WaitAsync();

            // NOTE: we may have closed during the wait!
            if (Interlocked.Read(ref _closed) != 0) return;

            _val = value;
            _out?.Release();
        }

        public void Close()
        {
            lock (_closerLock)
            {
                if (Interlocked.Read(ref _closed) != 0) return;

                Interlocked.CompareExchange(ref _closed, 1, 0);
                _in.Dispose();
                _in = null!;
            }
        }
    }
}
