using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Core
{
    internal abstract class TaskSafeDisposable<T> : IDisposable
    {
        public sealed class BoundActionField<T>
        {
            internal sealed class BoundAction : IBoundAction
            {
                private readonly Action<T> _act;
                private readonly T _context;

                public BoundAction(Action<T> action, T context)
                {
                    _act = action;
                    _context = context;
                }

                public BoundAction(BoundAction og, Func<T, T> contextUpdater)
                {
                    _act = og._act;
                    _context = contextUpdater(og._context);
                }

                public void Invoke() => _act?.Invoke(_context);
            }

            public interface IBoundAction
            {
                public void Invoke();
            }

            private BoundAction? _field;

            public BoundActionField(Action<T> action, T context)
            {
                _field = new BoundAction(action, context);
            }

            public bool IsEmpty => Interlocked.CompareExchange(ref _field, null, null) == null;

            public IBoundAction? TryGetAndUnset() => Interlocked.Exchange(ref _field, null);
        }

        private readonly BoundActionField<T> _context;
        private readonly TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();

        protected TaskSafeDisposable(T context)
        {
            _context = new BoundActionField<T>(Dispose, context);
        }

        public bool IsDisposeStarted => _context.IsEmpty;

        public bool IsDisposed => _tcs.Task.IsCompleted;

        public bool IsDisposing => IsDisposeStarted && !IsDisposed;

        protected abstract void Dispose(T context);

        public void Dispose()
        {
            var context = _context.TryGetAndUnset();
            if (context == null)
            {
                _tcs.Task.GetAwaiter().GetResult();
                return;
            }

            try
            {
                context.Invoke();
            }
            finally
            {
                _tcs.TrySetResult(null!);
            }
        }
    }
}
