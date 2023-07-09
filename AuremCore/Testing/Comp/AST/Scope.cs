using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Testing.Comp.AST
{
    internal class Scope
    {
        public List<Symbol> Declarations;
        public int Level;
        public Scope Parent;
    }
}
