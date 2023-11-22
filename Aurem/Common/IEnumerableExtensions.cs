using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Common
{
    public static class IEnumerableExtensions
    {
        /// <summary>
        /// Returns a tuple (index, value) for the enumerable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="e"></param>
        /// <returns></returns>
        public static IEnumerable<(int, T)> Index<T>(this IEnumerable<T> e)
        {
            var n = e.GetEnumerator();
            int i = 0;
            while (n.MoveNext())
            {
                yield return (i++, n.Current);
            }
        }
    }
}
