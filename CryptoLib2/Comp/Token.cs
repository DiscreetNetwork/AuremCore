using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoLib2.Comp
{
    internal enum Token: int
    {
        Undefined = 0,

        // lits and idents
        Identifier,
        Literal,
        String,
        
        // keywords
        Signal,
        Input,
        Output,
        Public,
        Template,
        Component,
        Var,
        Function,
        Return,
        If,
        Else,
        For,
        While,
        Do,
        Log,
        Assert,
        Include,
        Pragma,

        // symbols
        Lparen,
        Rparen,
        Lbrack,
        Rbrack,
        Lbrace,
        Rbrace,
        Semi,
        Comma,
        Period,

        // Assignment and Constraints
        Assign,
        LeftAssign,
        RightAssign,
        LeftConstrain,
        RightConstrain,
        Constrain,

        // Operators
        BoolAnd,
        BoolOr,
        Not,
        Add,
        Sub,
        Mul,
        Pow,
        Div,
        IDiv,
        Mod,
        Inc,
        Dec,
        BitAnd,
        BitOr,
        BitNot,
        BitXor,
        BitShl,
        BitShr,
        Qmark,
        Colon,

        // Comparison
        Gtr,
        Geq,
        Leq,
        Lss,
        Eq,
        Neq,

        // OpAssign
        AddAsgn,
        SubAsgn,
        MulAsgn,
        PowAsgn,
        DivAsgn,
        IDivAsgn,
        ModAsgn,
        AndAsgn,
        OrAsgn,
        NotAsgn,
        XorAsgn,
        ShlAsgn,
        ShrAsgn,

        // End
        EOF = 0xff,
    }
}
