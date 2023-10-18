using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Core.Extensions
{
    public static class IListExtensions
    {
        private static readonly Random rng = new Random();
        /// <summary>
        /// Shuffles over an <see cref="IList{T}"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IList<T> Shuffle<T>(this IList<T> source)
        {
            var dest = new List<T>(source);
            int n = source.Count;
            while (n > 1)
            {
                n--;
                var k = rng.Next(n + 1);
                (dest[k], dest[n]) = (dest[n], dest[k]);
            }

            return source;
        }

        public static byte[] GenerateRandomBytePerItem<T>(this IList<T> source)
        {
            var rv = new byte[source.Count];
            rng.NextBytes(rv);
            return rv;
        }
    }
}
