using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Model.Exceptions
{
    /// <summary>
    /// ComplianceException is raised when encountering a unit that does not follow compliance rules. Indicates a problem with both the process providing the data and the unit's creator.
    /// </summary>
    public class ComplianceException : Exception
    {
        public ComplianceException(string msg) : base("ComplianceException: " + msg) 
        { 
        
        }
    }
}
