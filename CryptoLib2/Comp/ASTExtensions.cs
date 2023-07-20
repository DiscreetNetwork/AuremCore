using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoLib2.Comp
{
    internal static class ASTExtensions
    {
        public static string ToStringO(this VarType typ)
        {
            return typ switch
            {
                VarType.Var => "var",
                VarType.SignalInput => "signal input",
                VarType.SignalOutput => "signal output",
                VarType.SignalIntermediate => "signal",
                VarType.Component => "component",
                _ => "",
            };
        }

        public static string ToStringO(this Op op) 
        {
            return op switch
            {
                Op.BitXor => "^",
                Op.BitAnd => "&",
                Op.BitOr => "|",
                Op.BitNot => "~",
                Op.And => "&&",
                Op.Or => "||",
                Op.Not => "!",
                Op.Neg => "-",
                Op.Pow => "**",
                Op.Mul => "*",
                Op.Div => "/",
                Op.Mod => "%",
                Op.IntDiv => "\\",
                Op.Add => "+",
                Op.Sub => "-",
                Op.ShiftL => "<<",
                Op.ShiftR => ">>",
                Op.Eq => "==",
                Op.Neq => "!=",
                Op.Lesser => "<",
                Op.Greater => ">",
                Op.GreaterEq => ">=",
                Op.LesserEq => "<=",
                _ => "",
            };
        }

        public static string ToStringO(this AssignOp op)
        {
            return op switch
            {
                AssignOp.VarAssign => "=",
                AssignOp.SignalAssign => "<--",
                AssignOp.ConstrainAssign => "<==",
            };
        }
    }
}
