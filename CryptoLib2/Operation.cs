using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoLib2
{
    internal class Operation
    {
        public SimpleProgramSymbol lhs;
        public SimpleProgramSymbol rhs1;
        public SimpleProgramSymbol rhs2;

        public Op op;

        public Operation() { }

        public Operation(SimpleProgramSymbol lhs, SimpleProgramSymbol rhs1, SimpleProgramSymbol rhs2, Op op)
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
