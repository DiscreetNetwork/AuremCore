using AuremCore.Core;
using BN256Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Model
{
    /// <summary>
    /// Contains utility methods for the Model namespace.
    /// </summary>
    public static class ModelUtils
    {
        /// <summary>
        /// Produces a <see cref="Preblock"/> from a slice of units containing a timing round. It assumes the timing unit is the last unit in the slice, and that random source of data of the timing unit starts with random bytes from the previous level.
        /// </summary>
        /// <param name="round"></param>
        /// <returns></returns>
        public static Preblock ToPreblock(IList<IUnit> round)
        {
            var data = new List<byte[]>(round.Count);
            foreach (var u in round)
            {
                if (u.Dealing())
                {
                    data.Add(u.Data());
                }
            }

            var randomBytes = round[^1].RandomSourceData()[0..Constants.SignatureLength];
            return new Preblock(data, randomBytes);
        }
    }
}
