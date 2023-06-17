using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Testing
{
    internal class Operation
    {
        public Symbol lhs;
        public Symbol rhs1;
        public Symbol rhs2;

        public Op op;

        public Operation() { }

        public Operation(Symbol lhs, Symbol rhs1, Symbol rhs2, Op op)
        {
            if (rhs2 == null && op != Op.ASSIGN)
            {
                throw new ArgumentException();
            }

            this.lhs = lhs;
            this.rhs1 = rhs1;
            this.rhs2 = rhs2;
            this.op = op;
        }
    }
}
