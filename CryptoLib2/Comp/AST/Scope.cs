using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoLib2.Comp.AST
{
    internal class Scope
    {
        public List<Symbol> Declarations;
        public int Level;
        public Scope Parent;

        //public List<>
    }
}
