using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Core
{
    public class TaskDisposable<T> where T : IDisposable
    {
        private readonly Task<T> _task;

        public TaskDisposable(Task<T> task)
        {
            _task = task ?? throw new ArgumentNullException(nameof(task));
        }

        public Task<T> AsTask() => _task;

        public static implicit operator Task<T>(TaskDisposable<T> src) => src.AsTask();

        public TaskAwaiter<T> GetAwaiter() => _task.GetAwaiter();

        public ConfiguredTaskAwaitable<T> ConfigureAwait(bool continueOnCapturedContext) => _task.ConfigureAwait(continueOnCapturedContext);
    }
}
