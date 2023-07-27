using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Model.Exceptions
{
    /// <summary>
    /// UnknownParentsException is an exception-like object used when trying to add a unit whose parents are not in the DAG.
    /// </summary>
    public class UnknownParentsException : Exception
    {
        public int Amount { get; }

        public UnknownParentsException(int amount) : base("Unknown parents " + amount.ToString())
        {
            Amount = amount;
        }
    }
}
