using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Model
{
    /// <summary>
    /// SlottedUnits defines a container for storing lists of units and accessing them using their creator's id.
    /// </summary>
    public interface ISlottedUnits
    {
        /// <summary>
        /// Get all units in this container created by the process with the given id. 
        /// Note that in the implementation, for efficiency reasons, modifications to the returned value results in undefined behavior.
        /// </summary>
        /// <param name="proc"></param>
        /// <returns></returns>
        public List<IUnit> Get(ushort proc);

        /// <summary>
        /// Replaces all units in this container created by the process, with the given id, with the given units.
        /// </summary>
        /// <param name="proc"></param>
        /// <param name="units"></param>
        public void Set(ushort proc, List<IUnit> units);

        /// <summary>
        /// Iterates through all units in the container, in chunks coressponding to different creator ids, until the given function returns false.
        /// </summary>
        /// <param name="iteration"></param>
        public void Iterate(Func<List<IUnit>, bool> iteration);
    }
}
