using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Model.Exceptions
{
    /// <summary>
    /// DuplicatePreunit is an exception-like object used when encountering a preunit that is already known. Usually not a problem.
    /// </summary>
    public class DuplicatePreunitException : Exception
    {
        public IPreunit Preunit { get; }

        public DuplicatePreunitException(IPreunit preunit) : base("Unit already in adder.")
        {
            Preunit = preunit;
        }
    }
}