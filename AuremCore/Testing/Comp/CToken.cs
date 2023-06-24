using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Testing.Comp
{
    internal struct CToken
    {
        public readonly Token Tok;
        public readonly string Val;
        public readonly int Line, Col;

        public CToken(int l, int c, Token tok, string val = "")
        {
            Line = l;
            Col = c;
            Tok = tok;
            Val = val;
        }
    }
}
