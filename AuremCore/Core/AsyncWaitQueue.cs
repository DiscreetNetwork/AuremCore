using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Core
{
    internal class AsyncWaitQueue<T>
    {
        private readonly IndexQueue<TaskCompletionSource<T>> _queue = new();

        public int Count => _queue.Count;

        public bool IsEmpty => Count == 0;

        public Task<T> Enqueue()
        {
            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            _queue.Enqueue(tcs);
            return tcs.Task;
        } 

        public void Dequeue(T? result)
        {
            _queue.Dequeue().TrySetResult(result!);
        }

        public void DequeueAll(T? result)
        {
            foreach (var src in _queue) src.TrySetResult(result!);

            _queue.Clear();
        }

        public bool TryCancel(Task task, CancellationToken token)
        {
            for (int i = 0; i < _queue.Count; i++)
            {
                if (_queue[i].Task == task)
                {
                    _queue[i].TrySetCanceled(token);
                    _queue.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public void CancelAll(CancellationToken token)
        {
            foreach (var src in _queue) src.TrySetCanceled(token);

            _queue.Clear();
        }
    }

    internal static class AsyncWaitQueueExtensions
    {
        public static Task<T> Enqueue<T>(this AsyncWaitQueue<T> q, object mx, CancellationToken token)
        {
            if (token.IsCancellationRequested) return Task.FromCanceled<T>(token);

            var ret = q.Enqueue();
            if (!token.CanBeCanceled) return ret;

            var registration = token.Register(() =>
            {
                lock (mx) q.TryCancel(ret, token);
            }, useSynchronizationContext: false);
            ret.ContinueWith(t => registration.Dispose(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

            return ret;
        }
    }
}
