using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CryptoLib2.Comp
{
    internal enum Op
    {
        Undefined = 0,
        BitXor,
        BitAnd,
        BitOr,
        BitNot,
        And,
        Or,
        Not,
        Neg,
        Pow,
        Mul,
        Div,
        IntDiv,
        Mod,
        Add,
        Sub,
        ShiftL,
        ShiftR,
        Eq,
        Neq,
        Lesser,
        Greater,
        LesserEq,
        GreaterEq,
        Ternary,
        FuncCall,
        AnonymousComp,
        Literal,
        Variable,
        Array,
        Tuple,
        Inc,
        Dec,
        Assign,
        Constrain,
        SignalAssign,
        ConstrainAssign,
    }

    public enum AssignOp
    {
        Unknown,
        VarAssign,
        ConstrainAssign,
        SignalAssign,
    }

    public enum StmtType
    {
        Declaration,
        BlockDeclaration,
        InitializationBlock,
        InitializationBlockFull,
        Substitution,
        BlockSubstitution,
        Block,
        Cond,
        For,
        While,
        Return,
        Constrain,
        Log,
        Assert,
        Simple,
    }

    public enum VarType
    {
        Unknown,
        Var,
        SignalInput,
        SignalOutput,
        SignalIntermediate,
        Function,
        Component,
        AnonymousComponent,
    }

    public enum ExprType
    {
        InfixOp,
        PrefixOp,
        Cond,
        Var,
        Number,
        Call,
        AnonymousComp,
        InlineArray,
        Tuple,
        UniformArray,
    }

    internal class SourceExpression
    {
        public SourceExpression Lhs;
        public SourceExpression Rhs;
        public SourceExpression Cond;

        public Op Op; 

        public Variable Variable;
        public Literal Literal;
        public List<SourceExpression> Expressions;
        public List<(string, AssignOp, SourceExpression)> AnonArgs;

        public string ToString(bool declaration = false)
        {
            return Op switch
            {
                Op.Assign => $"{Lhs} = {Rhs}",
                Op.Inc => $"{Lhs}++",
                Op.Dec => $"{Lhs}--",
                Op.Ternary => $"{Cond} ? {Lhs} : {Rhs}",
                Op.Constrain => $"{Lhs} === {Rhs}",
                Op.SignalAssign => $"{Lhs} <-- {Rhs}",
                Op.ConstrainAssign => $"{Lhs} <== {Rhs}",
                Op.Array => $"[{string.Join(", ", Expressions.Select(x => x.ToString()))}]",
                Op.Literal => $"{(Literal.String == null ? Literal.Value.ToString() : $"\"{Literal.String}\"")}",
                Op.Variable => $"{Variable.ToString(declaration)}",
                Op.FuncCall => $"{Variable.Name}({string.Join(", ", Expressions.Select(x => x.ToString()))})",
                Op.AnonymousComp => $"{Variable.Name}({string.Join(", ", Expressions.Select(x => x.ToString()))})({string.Join(", ", AnonArgs.Select(x => x.ToString()))})",
                Op.Not => $"{Op.Not.ToStringO()}{Lhs}",
                Op.BitNot => $"{Op.BitNot.ToStringO()}{Lhs}",
                Op.Neg => $"{Op.Neg.ToStringO()}{Lhs}",
                Op.Tuple => "###tuple###",
                _ => $"({Lhs} {Op.ToStringO()} {Rhs})",
            };
        }

        public override string ToString()
        {
            return ToString(false);
        }
    }

    internal class Pragma
    {
        public string Directive;
        public VersionNum Version;

        public override string ToString() 
        {
            return $"pragma {Directive} {Version}";
        }
    }

    internal class VersionNum
    {
        public int Major;
        public int Minor;
        public int Build;

        public override string ToString()
        {
            return $"{Major}.{Minor}.{Build}";
        }
    }

    internal class Include
    {
        public string Path;

        public override string ToString()
        {
            return $"include \"{Path}\"";
        }
    }

    internal class MainComponent
    {
        public List<string> Publics;
        public SourceExpression Comp;

        public override string ToString()
        {
            return $"component main {{public [{string.Join(", ", Publics)}]}} = {Comp};";
        }
    }

    internal class SourceFunction
    {
        public string Name;
        public List<string> Args;
        public SourceStmt Body;

        public override string ToString()
        {
            return $"function {Name}({string.Join(", ", Args)}) {Body}";
        }
    }

    internal class SourceTemplate
    {
        public string Name;
        public List<string> Args;
        public SourceStmt Body;

        public override string ToString()
        {
            return $"template {Name}({string.Join(", ", Args)}) {Body}";
        }
    }

    internal class SourceStmt
    {
        public SourceStmt Stmt1; // for part 1, or true condition in if-then-else
        public SourceStmt Stmt2; // for part 3, or else condition in if-then-else
        public SourceStmt Arg;

        public SourceExpression Lhs; // cond in if, cond in while, cond in For, expr in return, expr in simple, expr in assert
        public SourceExpression Rhs;

        public StmtType Type;
        public AssignOp AssignOp;

        public List<SourceExpression> LhsTup; // also arguments in log
        public List<SourceExpression> RhsTup;

        public List<SourceStmt> Block; // blockstmt, InitializationBlock
        public SourceStmt Body; // if branch, body in for, while, etc.

        private static readonly string pad = "    ";

        public string ToString(string padding)
        {
            var rv = Type switch
            {
                StmtType.Constrain => $"{Lhs} === {Rhs};",
                StmtType.Assert => $"assert({Lhs});",
                StmtType.Simple => $"{Lhs};",
                StmtType.Declaration => $"{Lhs.Variable.ToString(true)} {(Rhs == null ? "" : AssignOp.ToStringO() + Rhs.ToString())};",
                StmtType.Substitution => $"{Lhs.Variable.ToString()} {AssignOp.ToStringO()} {Rhs};",
                StmtType.BlockDeclaration => $"{(LhsTup[0].Op == Op.Assign ? LhsTup[0].Lhs.Variable.VarType.ToStringO() : LhsTup[0].Variable.VarType.ToStringO())} {LhsTup.Select(x => x.ToString()).Aggregate("", (x, y) => x == "" ? y : x + ", " + y)};",
                StmtType.InitializationBlock => $"{(LhsTup[0].Op == Op.Assign ? LhsTup[0].Lhs.Variable.VarType.ToStringO() : LhsTup[0].Variable.VarType.ToStringO())} {LhsTup.Select(x => x.ToString()).Aggregate("", (x, y) => x == "" ? y : x + ", " + y)} {AssignOp} {Rhs};",
                StmtType.Return => $"return {Lhs};",
                StmtType.Log => $"log({string.Join(", ", LhsTup.Select(x => x.ToString()))});",
                StmtType.While => $"while ({Lhs}) {Body.ToString(padding)}",
                StmtType.For => $"for ({Stmt1} {Lhs}; {Stmt2.ToString()[..^1]}) {Body.ToString(padding)}",
                StmtType.Cond => $"if ({Lhs}) {Stmt1.ToString(padding)}" + (Stmt2 == null ? "" : $"else {Stmt2.ToString(padding)}"),
                StmtType.Block => $"{{\n{Block.Aggregate("", (x, y) => x + y.ToString(padding + pad) + "\n")}{padding}}}",
                _ => "",// FIXME
            };

            return Type == StmtType.Block ? rv : (padding + rv);
        }

        public List<SourceStmt> Simplify()
        {
            var ss = new List<SourceStmt>();

            switch (Type)
            {
                case StmtType.BlockDeclaration:
                    var type = (LhsTup[0].Op == Op.Assign) ? LhsTup[0].Lhs.Variable.VarType : LhsTup[0].Variable.VarType;
                    // collect declarations
                    ss.AddRange(LhsTup.Select(x => {
                        if (x.Op == Op.Assign) x.Lhs.Variable.VarType = type; else x.Variable.VarType = type;
                        return new SourceStmt { Lhs = x.Op == Op.Assign ? x.Lhs : x, Type = StmtType.Declaration };
                    }));
                    // collect substitutions
                    ss.AddRange(LhsTup.Where(x => x.Op == Op.Assign).Select(x => new SourceStmt { Lhs = x.Lhs, Rhs = x.Rhs, Type = StmtType.Substitution }));
                    break;
                case StmtType.InitializationBlock:
                    // TODO: implement
                default:
                    break;
            }

            // FIXME 
            return ss;
        }

        public override string ToString()
        {
            return ToString("");
        }
    }

    internal class Variable
    {
        public string Name;
        public VarType VarType = VarType.Unknown;
        public List<Access> Access;

        public string ToString(bool decl = false)
        {
            return $"{(decl ? $"{VarType.ToStringO()} " : "")}{Name}{string.Join("", Access.Select(x => x.ToString()))}";
        }

        public override string ToString()
        {
            return ToString(false);
        }
    }

    internal class Access
    {
        public string Subcomponent;
        public SourceExpression Index;

        public override string ToString()
        {
            return Index == null ? $".{Subcomponent}" : $"[{Index}]";
        }
    }

    internal class Literal
    {
        public BigInteger Value;
        public string String;
    }

    internal class SourceTree
    {
        public Pragma Direct;
        public List<Include> Includes = new();
        public List<SourceFunction> Functions = new();
        public List<SourceTemplate> Templates = new();
        public MainComponent Main;

        public override string ToString()
        {
            return $"{(Direct == null ? "" : Direct.ToString() + "\n")}{Includes.Aggregate("", (x, y) => x == "" ? x : (x + y.ToString() + "\n"))}{Functions.Aggregate("", (x, y) => x == "" ? x : (x + y.ToString() + "\n"))}{Templates.Aggregate("", (x, y) =>x + y.ToString() + "\n")}{(Main != null ? Main.ToString() + "\n" : "")}";
        }
    }
}
