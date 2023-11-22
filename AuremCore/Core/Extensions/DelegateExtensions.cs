using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Core.Extensions
{
    public static class DelegateExtensions
    {
        public static void InvokeAndCaptureException(this Action a, out Exception? ex)
        {
            try
            {
                a.Invoke();
                ex = null;
            }
            catch (Exception e)
            {
                ex = e;
            }
        }

        public static void InvokeAndCaptureException<T1>(this Action<T1> a, T1 arg1, out Exception? ex)
        {
            try
            {
                a.Invoke(arg1);
                ex = null;
            }
            catch (Exception e)
            {
                ex = e;
            }
        }

        public static void InvokeAndCaptureException<T1, T2>(this Action<T1, T2> a, T1 arg1, T2 arg2, out Exception? ex)
        {
            try
            {
                a.Invoke(arg1, arg2);
                ex = null;
            }
            catch (Exception e)
            {
                ex = e;
            }
        }

        public static void InvokeAndCaptureException<T1, T2, T3>(this Action<T1, T2, T3> a, T1 arg1, T2 arg2, T3 arg3, out Exception? ex)
        {
            try
            {
                a.Invoke(arg1, arg2, arg3);
                ex = null;
            }
            catch (Exception e)
            {
                ex = e;
            }
        }

        public static void InvokeAndCaptureException<T1, T2, T3, T4>(this Action<T1, T2, T3, T4> a, T1 arg1, T2 arg2, T3 arg3, T4 arg4, out Exception? ex)
        {
            try
            {
                a.Invoke(arg1, arg2, arg3, arg4);
                ex = null;
            }
            catch (Exception e)
            {
                ex = e;
            }
        }

        public static void InvokeAndCaptureException<T1, T2, T3, T4, T5>(this Action<T1, T2, T3, T4, T5> a, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, out Exception? ex)
        {
            try
            {
                a.Invoke(arg1, arg2, arg3, arg4, arg5);
                ex = null;
            }
            catch (Exception e)
            {
                ex = e;
            }
        }

        public static void InvokeAndCaptureException<T1, T2, T3, T4, T5, T6>(this Action<T1, T2, T3, T4, T5, T6> a, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, out Exception? ex)
        {
            try
            {
                a.Invoke(arg1, arg2, arg3, arg4, arg5, arg6);
                ex = null;
            }
            catch (Exception e)
            {
                ex = e;
            }
        }

        public static TResult InvokeAndCaptureException<TResult>(this Func<TResult> func, out Exception? ex)
        {
            try
            {
                var res = func.Invoke();
                ex = null;
                return res;
            }
            catch (Exception e)
            {
                ex = e;
                return default!;
            }
        }

        public static async Task<(TResult, Exception?)> InvokeAndCaptureExceptionAsync<TResult>(this Func<Task<TResult>> func)
        {
            try
            {
                var res = await func.Invoke();
                return (res, null);
            }
            catch (Exception e)
            {
                return (default!, e);
            }
        }

        public static async Task<Exception?> InvokeAndCaptureExceptionAsync(this Func<Task> func)
        {
            try
            {
                await func.Invoke();
                return null;
            }
            catch (Exception e)
            {
                return e;
            }
        }


        public static TResult InvokeAndCaptureException<T1, TResult>(this Func<T1, TResult> func, T1 arg1, out Exception? ex)
        {
            try
            {
                var res = func.Invoke(arg1);
                ex = null;
                return res;
            }
            catch (Exception e)
            {
                ex = e;
                return default!;
            }
        }

        public static async Task<(TResult, Exception?)> InvokeAndCaptureExceptionAsync<T1, TResult>(this Func<T1, Task<TResult>> func, T1 arg1)
        {
            try
            {
                var res = await func.Invoke(arg1);
                return (res, null);
            }
            catch (Exception e)
            {
                return (default!, e);
            }
        }

        public static async Task<Exception?> InvokeAndCaptureExceptionAsync<T1>(this Func<T1, Task> func, T1 arg1)
        {
            try
            {
                await func.Invoke(arg1);
                return null;
            }
            catch (Exception e)
            {
                return e;
            }
        }

        public static TResult InvokeAndCaptureException<T1, T2, TResult>(this Func<T1, T2, TResult> func, T1 arg1, T2 arg2, out Exception? ex)
        {
            try
            {
                var res = func.Invoke(arg1, arg2);
                ex = null;
                return res;
            }
            catch (Exception e)
            {
                ex = e;
                return default!;
            }
        }

        public static async Task<(TResult, Exception?)> InvokeAndCaptureExceptionAsync<T1, T2, TResult>(this Func<T1, T2, Task<TResult>> func, T1 arg1, T2 arg2)
        {
            try
            {
                var res = await func.Invoke(arg1, arg2);
                return (res, null);
            }
            catch (Exception e)
            {
                return (default!, e);
            }
        }

        public static async Task<Exception?> InvokeAndCaptureExceptionAsync<T1, T2>(this Func<T1, T2, Task> func, T1 arg1, T2 arg2)
        {
            try
            {
                await func.Invoke(arg1, arg2);
                return null;
            }
            catch (Exception e)
            {
                return e;
            }
        }

        public static TResult InvokeAndCaptureException<T1, T2, T3, TResult>(this Func<T1, T2, T3, TResult> func, T1 arg1, T2 arg2, T3 arg3, out Exception? ex)
        {
            try
            {
                var res = func.Invoke(arg1, arg2, arg3);
                ex = null;
                return res;
            }
            catch (Exception e)
            {
                ex = e;
                return default!;
            }
        }

        public static async Task<(TResult, Exception?)> InvokeAndCaptureExceptionAsync<T1, T2, T3, TResult>(this Func<T1, T2, T3, Task<TResult>> func, T1 arg1, T2 arg2, T3 arg3)
        {
            try
            {
                var res = await func.Invoke(arg1, arg2, arg3);
                return (res, null);
            }
            catch (Exception e)
            {
                return (default!, e);
            }
        }

        public static async Task<Exception?> InvokeAndCaptureExceptionAsync<T1, T2, T3>(this Func<T1, T2, T3, Task> func, T1 arg1, T2 arg2, T3 arg3)
        {
            try
            {
                await func.Invoke(arg1, arg2, arg3);
                return null;
            }
            catch (Exception e)
            {
                return e;
            }
        }

        public static TResult InvokeAndCaptureException<T1, T2, T3, T4, TResult>(this Func<T1, T2, T3, T4, TResult> func, T1 arg1, T2 arg2, T3 arg3, T4 arg4, out Exception? ex)
        {
            try
            {
                var res = func.Invoke(arg1, arg2, arg3, arg4);
                ex = null;
                return res;
            }
            catch (Exception e)
            {
                ex = e;
                return default!;
            }
        }

        public static async Task<(TResult, Exception?)> InvokeAndCaptureExceptionAsync<T1, T2, T3, T4, TResult>(this Func<T1, T2, T3, T4, Task<TResult>> func, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            try
            {
                var res = await func.Invoke(arg1, arg2, arg3, arg4);
                return (res, null);
            }
            catch (Exception e)
            {
                return (default!, e);
            }
        }

        public static async Task<Exception?> InvokeAndCaptureExceptionAsync<T1, T2, T3, T4>(this Func<T1, T2, T3, T4, Task> func, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            try
            {
                await func.Invoke(arg1, arg2, arg3, arg4);
                return null;
            }
            catch (Exception e)
            {
                return e;
            }
        }

        public static TResult InvokeAndCaptureException<T1, T2, T3, T4, T5, TResult>(this Func<T1, T2, T3, T4, T5, TResult> func, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, out Exception? ex)
        {
            try
            {
                var res = func.Invoke(arg1, arg2, arg3, arg4, arg5);
                ex = null;
                return res;
            }
            catch (Exception e)
            {
                ex = e;
                return default!;
            }
        }

        public static TResult InvokeAndCaptureException<T1, T2, T3, T4, T5, T6, TResult>(this Func<T1, T2, T3, T4, T5, T6, TResult> func, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, out Exception? ex)
        {
            try
            {
                var res = func.Invoke(arg1, arg2, arg3, arg4, arg5, arg6);
                ex = null;
                return res;
            }
            catch (Exception e)
            {
                ex = e;
                return default!;
            }
        }
    }
}
