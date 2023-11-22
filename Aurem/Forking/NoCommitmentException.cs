using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Forking
{
    public class NoCommitmentException : Exception
    {
        public NoCommitmentException(string msg) : base("MissingCommitment: " + msg) { }
    }
}
