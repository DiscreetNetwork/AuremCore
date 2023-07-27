using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Model
{
    /// <summary>
    /// Represents a source of randomness needed to run the consensus protocol.
    /// It specifies what kind of data should be included in units, and can use this data to generate random bytes.
    /// </summary>
    public interface IRandomSource
    {
        /// <summary>
        /// Returns random bytes for a given process and level.
        /// </summary>
        /// <param name="proc">The id of the process.</param>
        /// <param name="level">The level to provide randomness for.</param>
        /// <returns></returns>
        public byte[] RandomBytes(ushort proc, int level);

        /// <summary>
        /// Returns data which should be incluced in a unit based on its level and parents.
        /// </summary>
        /// <param name="parents">The parents of the unit which should include this data.</param>
        /// <param name="level">The level of the unit which should include this data.</param>
        /// <returns>The data to include in the unit.</returns>
        public byte[] DataToInclude(List<IUnit> parents, int level);
    }
}
