using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Model.Exceptions
{
    /// <summary>
    /// DuplicateUnit is an exception-like object used when encountering a unit that is already known. Usually not a problem.
    /// </summary>
    public class DuplicateUnitException : Exception
    {
        public IUnit Unit { get; }

        public DuplicateUnitException(IUnit unit) : base("Unit already in DAG.")
        {
            Unit = unit;
        }
    }
}
