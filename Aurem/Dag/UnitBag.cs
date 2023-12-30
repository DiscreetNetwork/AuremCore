using Aurem.Common;
using Aurem.Model;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Dag
{
    public class UnitBag
    {
        private ConcurrentDictionary<Hash, IUnit> Contents;

        public UnitBag()
        {
            Contents = new ConcurrentDictionary<Hash, IUnit>(new Hash.HashEqualityComparer());
        }

        public void Add(IUnit u) => Contents[u.Hash()] = u;

        public IUnit this[Hash h] => Contents[h];

        public IUnit GetOne(Hash h)
        {
            var success = Contents.TryGetValue(h, out var u);
            return success ? u! : null!;
        }

        public IUnit[] GetMany(IList<Hash> hs)
        {
            IUnit[] rv = new IUnit[hs.Count];
            foreach ((var i, var h) in hs.Index())
            {
                if (h.Equals(Hash.Empty)) continue;

                rv[i] = GetOne(h);
            }

            return rv;
        }

        public List<IUnit> GetAll()
        {
            return Contents.Values.ToList();
        }

        public IEnumerator<KeyValuePair<Hash, IUnit>> GetEnumerator() => Contents.GetEnumerator();
    }
}
