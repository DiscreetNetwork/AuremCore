using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Model.Exceptions
{
    /// <summary>
    /// DataException represents incorrect data received from a process. Indicates a problem with the process providing the data.
    /// </summary>
    public class DataException : Exception
    {
        public DataException(string msg) : base("DataException: " + msg) { }
    }
}
