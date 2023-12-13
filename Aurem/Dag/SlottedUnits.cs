using Aurem.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Dag
{
    public class SlottedUnits : ISlottedUnits
    {
        private ConcurrentDictionary<ushort, List<IUnit>> Contents;
        private readonly object[] locks;
        private ushort Max;
        private List<IUnit> Empty;

        public SlottedUnits(ushort n)
        {
            Contents = new();
            foreach (ushort i in Enumerable.Range(0, n).Select(v => (ushort)v))
            {
                Contents.TryAdd(i, null!);
            }
            Max = n;
            Empty = new();
            locks = Enumerable.Range(0, n).Select(x => new object()).ToArray();
        }

        /// <summary>
        /// Returns the units at the provided ID. 
        /// WARNING: modifying the returned value directly results in undefined behavior!
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public List<IUnit> Get(ushort id)
        {
            if (id >= Max)
            {
                return Empty;
            }

            lock (locks[id])
            {
                var success = Contents.TryGetValue(id, out var result);
                if (success && result != null) return result;
            }

            return Empty;
        }

        /// <summary>
        /// Replaces the units at the provided ID with units.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="value"></param>
        public void Set(ushort id, List<IUnit> units)
        {
            if (id >= Max) return;

            lock (locks[id])
            {
                Contents[id] = units;
            }
        }

        /// <summary>
        /// Runs work on its contents consecutively, until it returns false or the contents run out.
        /// Attempts to synchronize on values, but modifying returned values in Get() will cause unwanted behavior if done concurrently.
        /// </summary>
        /// <param name="work"></param>
        public void Iterate(Func<List<IUnit>, bool> work)
        {
            foreach (var k in Contents.Keys)
            {
                lock (locks[k])
                {
                    if (!work(Contents[k] ?? Empty)) return;
                }
            }
        }
    }
}
