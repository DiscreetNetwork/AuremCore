using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Dag
{
    public class NoSuchFiberException : Exception
    {
        public int Value;

        public NoSuchFiberException(int value) : base($"value {value} does not exist") { Value = value; }
    }
}
