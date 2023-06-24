using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Testing.Comp
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

    internal class Expression
    {
        public Expression Lhs;
        public Expression Rhs;
        public Expression Cond;

        public Op Op; 

        public Variable Variable;
        public Literal Literal;
        public List<Expression> Expressions;
        public List<(string, AssignOp, Expression)> AnonArgs;

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
        public Expression Comp;

        public override string ToString()
        {
            return $"component main {{public [{string.Join(", ", Publics)}]}} = {Comp};";
        }
    }

    internal class Function
    {
        public string Name;
        public List<string> Args;
        public Stmt Body;

        public override string ToString()
        {
            return $"function {Name}({string.Join(", ", Args)}) {Body}";
        }
    }

    internal class Template
    {
        public string Name;
        public List<string> Args;
        public Stmt Body;

        public override string ToString()
        {
            return $"template {Name}({string.Join(", ", Args)}) {Body}";
        }
    }

    internal class Stmt
    {
        public Stmt Stmt1; // for part 1, or true condition in if-then-else
        public Stmt Stmt2; // for part 3, or else condition in if-then-else
        public Stmt Arg;

        public Expression Lhs; // cond in if, cond in while, cond in For, expr in return, expr in simple, expr in assert
        public Expression Rhs;

        public StmtType Type;
        public AssignOp AssignOp;

        public List<Expression> LhsTup; // also arguments in log
        public List<Expression> RhsTup;

        public List<Stmt> Block; // blockstmt, InitializationBlock
        public Stmt Body; // if branch, body in for, while, etc.

        private static readonly string pad = "    ";

        public string ToString(string padding)
        {
            var rv = Type switch
            {
                StmtType.Constrain => $"{Lhs} === {Rhs};",
                StmtType.Assert => $"assert({Lhs});",
                StmtType.Simple => $"{Lhs};",
                StmtType.Declaration => $"{Lhs.Variable.ToString(true)} {AssignOp.ToStringO()} {Rhs};",
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
        public Expression Index;

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

    internal class AST
    {
        public Pragma Direct;
        public List<Include> Includes = new();
        public List<Function> Functions = new();
        public List<Template> Templates = new();
        public MainComponent Main;

        public override string ToString()
        {
            return $"{(Direct == null ? "" : Direct.ToString() + "\n")}{Includes.Aggregate("", (x, y) => x == "" ? x : (x + y.ToString() + "\n"))}{Functions.Aggregate("", (x, y) => x == "" ? x : (x + y.ToString() + "\n"))}{Templates.Aggregate("", (x, y) =>x + y.ToString() + "\n")}{(Main != null ? Main.ToString() + "\n" : "")}";
        }
    }
}
