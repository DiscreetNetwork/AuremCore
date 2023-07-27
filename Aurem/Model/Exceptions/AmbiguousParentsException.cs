using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Model.Exceptions
{
    /// <summary>
    /// AmbiguousParentsException is an error-like object used when trying to add a unit whose parents cannot be determined by pid and height.
    /// It contains all possible parents sliced by creator.
    /// </summary>
    public class AmbiguousParentsException : Exception
    {
        public IUnit[][] Units { get; }

        public AmbiguousParentsException(IUnit[][] units) : base("Ambiguous parents")
        {
            Units = units;
        }
    }
}
