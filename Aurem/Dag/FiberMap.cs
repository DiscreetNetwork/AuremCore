using Aurem.Model;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Dag
{
    public class FiberMap
    {
        private Dictionary<int, ISlottedUnits> Content;
        private readonly object _accessLock = new object();
        private ushort Width;
        private long _length;

        public FiberMap(ushort width, int initialLength)
        {
            Width = width;
            _length = initialLength;
            Content = new();

            for (int i = 0; i < initialLength; i++)
            {
                Content[i] = new SlottedUnits(width);
            }
        }

        public (ISlottedUnits?, Exception?) GetFiber(int value)
        {
            lock (_accessLock)
            {
                var success = Content.TryGetValue(value, out var result);
                if (!success) return (null, new NoSuchFiberException(value));

                return (result, null);
            }
        }

        public int Length() => (int)Interlocked.Read(ref _length);

        public void ExtendBy(int nValues)
        {
            lock ( _accessLock)
            {
                var newMaxLen = nValues + Length();
                for (int i = Length(); i < newMaxLen; i++)
                {
                    if (!Content.ContainsKey(i)) Content[i] = new SlottedUnits(Width);
                }
            }

            Interlocked.Add(ref _length, nValues);
        }

        /// <summary>
        /// Takes a list of heights (MUST BE NPROC LENGTH) and returns a collection (of length NProc) of lists of corresponding units.
        /// The second returned value is the number of unknown units (no units for that creator-height pair).
        /// </summary>
        /// <param name="heights"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public (IList<List<IUnit>>, int) Get(IList<int> heights)
        {
            if (Width != heights.Count) throw new ArgumentException("wrong number of heights passed to fiber map", nameof(heights));

            var result = new List<IUnit>[Width];
            var unknown = 0;
            // precheck all values
            lock ( _accessLock)
            {
                var toGet = Enumerable.Range(0, Width).Select(x => (x, heights[x], heights[x] != -1 && Content.ContainsKey(heights[x]))).Where(x => x.Item3).Select(x => (x.Item1, x.Item2));
                foreach ((var pid, var h) in toGet)
                {
                    result[pid] = Content[h].Get((ushort)pid);
                    if (result[pid].Count == 0) unknown++;
                }

                return (result, unknown);
            }
        }

        public List<IUnit> Above(IList<int> heights)
        {
            if (heights.Count != Width) throw new ArgumentException("wrong number of heights passed to fiber map", nameof(heights));

            var min = heights.Min();
            var result = new List<IUnit>();
            lock ( _accessLock)
            {
                var length = _length;
                for (var height = min + 1; height < length; height++)
                {
                    var su = Content[height];
                    for (ushort i = 0; i < Width; i++)
                    {
                        result.AddRange(su.Get(i));
                    }
                }

                return result;
            }
        }

        public void Iterate(Func<int, ISlottedUnits, bool> predicate)
        {
            lock (_accessLock)
            {
                foreach ((var k, var su) in Content)
                {
                    if (!predicate(k, su)) break;
                }
            }
        }
    }
}
