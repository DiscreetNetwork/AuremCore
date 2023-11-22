using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CryptoLib2.Comp.AST
{
    public enum SymbolType
    {
        TemplateParameter,
        Variable,
        Signal,
        Component,
        Literal,
    }

    internal class Symbol
    {
        public List<Symbol> Dimension;
        public BigInteger Value;
        public string Name;
        public SymbolType Type;

        public bool IsConstant => Name == "";
        public bool IsArray => Dimension != null && Dimension.Count > 0;
    }
}
