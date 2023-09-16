using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Core.Extensions
{
    public static class TaskExtensions
    {
        private static class ExceptionHelpers
        {
            public static Exception PrepareForRethrow(Exception ex)
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
                return ex;
            }
        }

        public static void WaitAndUnwrapException(this Task task)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));
            task.GetAwaiter().GetResult();
        }

        public static void WaitAndUnwrapException(this Task task, CancellationToken token)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));

            try
            {
                task.Wait(token);
            }
            catch (AggregateException ex)
            {
                throw ExceptionHelpers.PrepareForRethrow(ex.InnerException);
            }
        }

        public static T WaitAndUnwrapException<T>(this Task<T> task)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));
            return task.GetAwaiter().GetResult();
        }

        public static T WaitAndUnwrapException<T>(this Task<T> task, CancellationToken token)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));

            try
            {
                task.Wait(token);
                return task.Result;
            }
            catch (AggregateException ex)
            {
                throw ExceptionHelpers.PrepareForRethrow(ex.InnerException);
            }
        }

        public static void WaitWithoutException(this Task task)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));

            try
            {
                task.Wait();
            }
            catch (AggregateException) { }
        }

        public static void WaitWithoutException(this Task task, CancellationToken token)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));

            try
            {
                task.Wait(token);
            }
            catch (AggregateException)
            {
                token.ThrowIfCancellationRequested();
            }
        }
    }
}
