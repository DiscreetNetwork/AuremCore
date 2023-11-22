using AuremCore.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Core
{
    /// <summary>
    /// Provides a means of shared read access and single-write access to asynchronous routines. 
    /// Read locks are shared and block calls to get the write lock.
    /// 
    /// </summary>
    public class AsyncRWLock
    {
        private sealed class WKey : TaskSafeDisposable<AsyncRWLock>
        {
            public WKey(AsyncRWLock _lock) : base(_lock) { }

            protected override void Dispose(AsyncRWLock context)
            {
                context.ReleaseWriterLock();
            }
        }

        private sealed class RKey : TaskSafeDisposable<AsyncRWLock>
        {
            public RKey(AsyncRWLock _lock) : base(_lock) { }

            protected override void Dispose(AsyncRWLock context)
            {
                context.ReleaseReaderLock();
            }
        }

        private readonly AsyncWaitQueue<IDisposable> writerQueue;

        private readonly AsyncWaitQueue<IDisposable> readerQueue;

        private readonly object _mutex = new();

        private int _locksHeld;

        public RWLockState State
        {
            get
            {
                var lh = _locksHeld;
                if (lh == 0) return RWLockState.Unlocked;
                if (lh == -1) return RWLockState.WriteLocked;
                
                return RWLockState.ReadLocked;
            }
        }

        public int ReaderLocks => _locksHeld == -1 ? 0 : _locksHeld;

        public bool WriterLocked => _locksHeld == -1;

        public enum RWLockState
        {
            Unlocked,
            ReadLocked,
            WriteLocked
        }

        public AsyncRWLock()
        {
            writerQueue = new AsyncWaitQueue<IDisposable>();
            readerQueue = new AsyncWaitQueue<IDisposable>();
        }

        private void ReleaseWaitersWhenCancelled(Task task)
        {
            task?.ContinueWith(t =>
            {
                lock (_mutex) ReleaseWaiters();
            }, CancellationToken.None, TaskContinuationOptions.OnlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        private Task<IDisposable> RequestReaderLockAsync(CancellationToken token)
        {
            lock (_mutex)
            {
                if (_locksHeld >= 0 && writerQueue.IsEmpty)
                {
                    _locksHeld++;
                    return Task.FromResult<IDisposable>(new RKey(this));
                }
                else
                {
                    return readerQueue.Enqueue(_mutex, token);
                }
            }
        }

        public TaskDisposable<IDisposable> ReaderLockAsync(CancellationToken token)
        {
            return new TaskDisposable<IDisposable>(RequestReaderLockAsync(token));
        }

        public TaskDisposable<IDisposable> ReaderLockAsync() => ReaderLockAsync(CancellationToken.None);

        public IDisposable ReaderLock(CancellationToken token)
        {
            return RequestReaderLockAsync(token).WaitAndUnwrapException();
        }

        public IDisposable ReaderLock() => ReaderLock(CancellationToken.None);

        private Task<IDisposable> RequestWriterLockAsync(CancellationToken token)
        {
            Task<IDisposable> ret;
            lock (_mutex)
            {
                if(_locksHeld == 0)
                {
                    _locksHeld = -1;
                    ret = Task.FromResult<IDisposable>(new WKey(this));
                }
                else
                {
                    ret = writerQueue.Enqueue(_mutex, token);
                }
            }

            ReleaseWaitersWhenCancelled(ret);
            return ret;
        }

        public TaskDisposable<IDisposable> WriterLockAsync(CancellationToken token)
        {
            return new TaskDisposable<IDisposable>(RequestWriterLockAsync(token));
        }

        public TaskDisposable<IDisposable> WriterLockAsync() => WriterLockAsync(CancellationToken.None);

        public IDisposable WriterLock(CancellationToken token)
        {
            return RequestWriterLockAsync(token).WaitAndUnwrapException();
        }

        public IDisposable WriterLock() => WriterLock(CancellationToken.None);

        private void ReleaseWaiters()
        {
            if (_locksHeld == -1) return;

            if (!writerQueue.IsEmpty)
            {
                if (_locksHeld == 0)
                {
                    _locksHeld = -1;
                    writerQueue.Dequeue(new WKey(this));
                    return;
                }
            }
            else
            {
                while (!readerQueue.IsEmpty)
                {
                    readerQueue.Dequeue(new RKey(this));
                    _locksHeld++;
                }
            }
        }

        internal void ReleaseReaderLock()
        {
            lock (_mutex)
            {
                _locksHeld--;
                ReleaseWaiters();
            }
        }

        internal void ReleaseWriterLock()
        {
            lock (_mutex)
            {
                _locksHeld = 0;
                ReleaseWaiters();
            }
        }
    }
}
