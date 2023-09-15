using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Core
{
    public class WaitGroup
    {
        private int _wg;
        private readonly object _lock = new object();

        private bool _done = true;
        CancellationTokenSource _wait;
        CancellationToken _token;
        TaskCompletionSource<bool> _waitAsync;
        // FIXME: async!

        public WaitGroup()
        {
            _wg = 0;
        }

        public void Add(int x)
        {
            lock (_lock)
            {
                if (_done)
                {
                    _wait = new CancellationTokenSource();
                    _token = _wait.Token;
                    _waitAsync = new TaskCompletionSource<bool>();
                    _done = false;
                    _token.Register(() => {
                        _waitAsync.SetResult(true);
                    });
                }
            }

            var n = Interlocked.Add(ref _wg, x);
            if (n <= 0)
            {
                lock (_lock)
                {
                    _done = true;
                    _wait.Cancel();
                }
            }
        }

        public void Done() { Add(-1); }

        public void Add()
        {
            Add(1);
        }

        public void Wait()
        {
            _wait.Token.WaitHandle.WaitOne();
        }

        public async Task WaitAsync(CancellationToken token = default)
        {
            await _waitAsync.Task;
        }
    }
}
