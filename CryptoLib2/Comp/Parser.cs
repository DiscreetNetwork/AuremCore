using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CryptoLib2.Comp
{
    internal class Parser : SourceReader
    {
        public SourceTree Ast;
        public Parser(string pth) : base(pth) { }

        public Parser(string content, bool path) : base(content, path) { }

        protected bool Got(Token tok)
        {
            if (token.Tok == tok)
            {
                Next();
                return true;
            }
            return false;
        }

        protected void Want(Token tok)
        {
            if (!Got(tok))
            {
                Error($"Expected {tok}; wanted {token.Tok}");
            }
        }

        protected bool GotAssignOp(out AssignOp op)
        {
            // consumes the token; short circuiting has our back
            if (Got(Token.Assign)) {
                op = AssignOp.VarAssign;
                return true;
            }
            else if (Got(Token.LeftAssign)) {
                op = AssignOp.SignalAssign;
                return true;
            }
            else if (Got(Token.LeftConstrain))
            {
                op = AssignOp.ConstrainAssign;
                return true;
            }

            op = AssignOp.Unknown;
            return false;
        }

        protected Pragma ParsePragma()
        {
            if (Got(Token.Pragma))
            {
                var dir = new Pragma();
                if (token.Tok != Token.Identifier)
                {
                    Error("Expected identifier");
                }
                dir.Directive = token.Val;
                Next();
                if (token.Tok == Token.Literal)
                {
                    var ver = new VersionNum();
                    ver.Major = int.Parse(token.Val);
                    Next();
                    Want(Token.Period);
                    ver.Minor = int.Parse(token.Val);
                    Next();
                    Want(Token.Period);
                    ver.Build = int.Parse(token.Val);
                    Next();

                    dir.Version = ver;
                }

                Want(Token.Semi);
                return dir;
            }

            return null;
        }

        public SourceTree Parse()
        {
            Ast = new SourceTree();

            Ast.Direct = ParsePragma();
            Next();
            while (token.Tok != Token.EOF)
            {
                switch (token.Tok)
                {
                    case Token.Include:
                        Ast.Includes.Add(ParseInclude());
                        break;
                    case Token.Template:
                        Ast.Templates.Add(ParseTemplate());
                        break;
                    case Token.Function:
                        Ast.Functions.Add(ParseFunction());
                        break;
                    case Token.Component:
                        Ast.Main = ParseMain();
                        break;
                    default:
                        Error("Expected a top-level definition");
                        SkipToGood();
                        break;
                }
            }
            
            return Ast;
        }

        protected void SkipToGood()
        {
            while (token.Tok != Token.EOF && token.Tok != Token.Semi && token.Tok != Token.Include && token.Tok != Token.Template && token.Tok != Token.Function && token.Tok != Token.Component)
            {
                Next();
            }
            Got(Token.Semi);
        }

        protected List<string> ParseIdentList()
        {
            List<string> idents = new();
            if (token.Tok == Token.Identifier)
            {
                idents.Add(token.Val);
                Next();
                while (Got(Token.Comma))
                {
                    idents.Add(token.Val);
                    Next();
                }
            }

            return idents;
        }

        protected string ParseName()
        {
            if (token.Tok == Token.Identifier)
            {
                var rv = token.Val;
                Want(Token.Identifier);
                return rv;
            }
            else
            {
                Error("Expected identifier (name)");
                return "";
            }
        }

        protected Include ParseInclude()
        {
            Include include = new Include();
            Want(Token.Include);
            if (token.Tok == Token.String)
            {
                include.Path = token.Val;
                Want(Token.Semi);
            }
            else
            {
                Error($"unexpected {token.Tok} in include");
                SkipToGood();
            }

            return include;
        }

        protected MainComponent ParseMain()
        {
            MainComponent main = new MainComponent();

            Want(Token.Component);
            if (token.Tok == Token.Identifier)
            {
                if (token.Val != "main")
                {
                    Error("Expected main component in top-level declaration");
                }
            }
            Want(Token.Identifier);

            if (Got(Token.Lbrace))
            {
                Want(Token.Public);
                Want(Token.Lbrack);
                main.Publics = ParseIdentList();
                Want(Token.Rbrack);
                Want(Token.Rbrace);
            }

            Want(Token.Assign);
            main.Comp = ParseExpr();

            Want(Token.Semi);

            return main;
        }

        protected SourceTemplate ParseTemplate()
        {
            SourceTemplate template = new SourceTemplate();

            Want(Token.Template);
            template.Name = ParseName();

            if (Got(Token.Lparen))
            {
                template.Args = ParseIdentList();
                Want(Token.Rparen);
            }

            template.Body = ParseBlockStmt();
            return template;
        }

        protected SourceFunction ParseFunction()
        {
            SourceFunction function = new SourceFunction();

            Want(Token.Function);
            function.Name = ParseName();

            Want(Token.Lparen);
            function.Args = ParseIdentList();
            Want(Token.Rparen);

            function.Body = ParseBlockStmt();
            return function;
        }

        protected List<string> ParseTagsList()
        {
            Want(Token.Lbrace);
            var rv = ParseIdentList();
            Want(Token.Rbrace);

            return rv;
        }

        protected VarType ParseSignalHeader()
        {
            VarType v = VarType.SignalIntermediate;
            Want(Token.Signal);
            if (Got(Token.Input))
            {
                v = VarType.SignalInput;
            }
            else if (Got(Token.Output))
            {
                v = VarType.SignalOutput;
            }

            if (Got(Token.Lbrace))
            {
                // FIXME: currently unused; refactor AST
                ParseTagsList();
            }

            return v;
        }

        protected SourceExpression ParseArrayAccess()
        {
            Want(Token.Lbrack);
            var e = ParseExpr();
            Want(Token.Rbrack);

            return e;
        }

        protected string ParseComponentAccess()
        {
            Want(Token.Period);
            return ParseName();
        }

        protected List<Access> ParseAccessList(bool restrictArrayAccess = false)
        {
            List<Access> rv = new List<Access>();
            while (true)
            {
                if (token.Tok == Token.Lbrack)
                {
                    rv.Add(new Access { Index = ParseArrayAccess() });
                }
                else if (!restrictArrayAccess && token.Tok == Token.Period)
                {
                    rv.Add(new Access { Subcomponent = ParseComponentAccess() });
                }
                else { break; }
            }

            return rv;
        }

        protected Variable ParseVariable()
        {
            var v = new Variable();
            v.Name = ParseName();
            v.Access = ParseAccessList();

            return v;
        }

        protected Variable ParseSimpleSymbol()
        {
            var v = new Variable();
            v.Name = ParseName();
            v.Access = ParseAccessList(true);
            v.VarType = VarType.Var;

            return v;
        }

        protected SourceExpression ParseComplexSymbol()
        {
            var v = ParseSimpleSymbol();
            Want(Token.Assign);
            SourceExpression rhs = ParseExpr();

            return new SourceExpression { Rhs = rhs, Lhs = new SourceExpression { Variable = v, Op = Op.Variable }, Op = Op.Assign };
        }

        protected SourceExpression ParseSignalConstraintSymbol()
        {
            var v = ParseSimpleSymbol();
            Want(Token.LeftConstrain);
            SourceExpression rhs = ParseExpr();

            return new SourceExpression { Rhs = rhs, Lhs = new SourceExpression { Variable = v, Op = Op.Variable }, Op = Op.ConstrainAssign };
        }

        protected SourceExpression ParseSignalSimpleSymbol(SourceExpression expr = null)
        {
            SourceExpression v;
            if (expr == null) v = new SourceExpression { Variable = ParseSimpleSymbol(), Op = Op.Variable };
            else v = expr;
            Want(Token.LeftAssign);
            SourceExpression rhs = ParseExpr();

            return new SourceExpression { Rhs = rhs, Lhs = v, Op = Op.SignalAssign };
        }

        protected SourceExpression ParseSymbol()
        {
            var v = new SourceExpression { Variable = ParseSimpleSymbol(), Op = Op.Variable };
            if (Got(Token.Assign))
            {
                v = new SourceExpression { Lhs = v, Rhs = ParseExpr(), Op = Op.Assign };
            }

            return v;
        }

        protected SourceExpression ParseSignalSymbol(out bool simple)
        {
            var v = new SourceExpression { Variable = ParseSimpleSymbol(), Op = Op.Variable };
            if (token.Tok == Token.LeftAssign)
            {
                simple = true;
                return v;
            }
            if (Got(Token.LeftConstrain))
            {
                v = new SourceExpression { Lhs = v, Rhs = ParseExpr(), Op = Op.ConstrainAssign };
            }

            simple = false;
            return v;
        }

        protected bool IsDeclaration()
        {
            return token.Tok == Token.Var || token.Tok == Token.Component || token.Tok == Token.Signal;
        }

        protected SourceStmt ParseDeclarationOrSubstitution(bool allowSimpleStmt = true, bool allowConstraint = false)
        {
            if (IsDeclaration())
            {
                return ParseDeclaration();
            }

            return ParseSubstitution(allowSimpleStmt, allowConstraint);
        }

        protected SourceStmt ParseDeclaration()
        {
            SourceStmt stmt = new SourceStmt();
            switch (token.Tok)
            {
                case Token.Var:
                    Next();
                    if (Got(Token.Lparen))
                    {
                        List<SourceExpression> lhs = new List<SourceExpression>();
                        while (!Got(Token.Rparen))
                        {
                            lhs.Add(new SourceExpression { Op = Op.Variable, Variable = ParseSimpleSymbol() });
                            if (token.Tok != Token.Rparen && !Got(Token.Comma))
                            {
                                Error($"unexpected {token.Tok} in list; expected comma or end of list");
                                break;
                            }
                        }
                        stmt.Type = StmtType.BlockDeclaration;
                        stmt.LhsTup = lhs;
                        if (GotAssignOp(out var _op))
                        {
                            stmt.Rhs = ParseExpr();
                            stmt.AssignOp = _op;
                            stmt.Type = StmtType.InitializationBlock;
                        }
                    }
                    else
                    {
                        List<SourceExpression> lhs = new List<SourceExpression>();
                        while (token.Tok != Token.Semi)
                        {
                            lhs.Add(ParseSymbol());
                            if (token.Tok != Token.Semi && !Got(Token.Comma))
                            {
                                Error($"unexpected {token.Tok} in list; expected comma or end of list");
                                break;
                            }
                        }
                        stmt.Type = StmtType.BlockDeclaration;
                        stmt.LhsTup = lhs;
                    }
                    break;
                case Token.Signal:
                    var styp = ParseSignalHeader();
                    if (Got(Token.Lparen))
                    {
                        List<SourceExpression> lhs = new List<SourceExpression>();
                        while (!Got(Token.Rparen))
                        {
                            lhs.Add(new SourceExpression { Op = Op.Variable, Variable = ParseSimpleSymbol() });
                            lhs[^1].Variable.VarType = styp;
                            if (token.Tok != Token.Rparen && !Got(Token.Comma))
                            {
                                Error($"unexpected {token.Tok} in list; expected comma or end of list");
                                break;
                            }
                        }
                        stmt.Type = StmtType.BlockDeclaration;
                        stmt.LhsTup = lhs;
                        if (GotAssignOp(out var _op))
                        {
                            stmt.Rhs = ParseExpr();
                            stmt.AssignOp = _op;
                            stmt.Type = StmtType.InitializationBlock;
                        }
                    }
                    else
                    {
                        List<SourceExpression> lhs = new List<SourceExpression>();
                        bool simple = false;
                        while (token.Tok != Token.Semi)
                        {
                            bool isSimple = false;
                            if (!simple) lhs.Add(ParseSignalSymbol(out isSimple));
                            if (isSimple)
                            {
                                simple = true;
                                // ensure full block is simple assign
                                var sim = lhs[^1];
                                lhs.RemoveAt(lhs.Count - 1);
                                lhs.Add(ParseSignalSimpleSymbol(sim));
                            }
                            else if (simple)
                            {
                                lhs.Add(ParseSignalSimpleSymbol());
                            }
                            if (lhs[^1].Op == Op.Variable)
                            {
                                lhs[^1].Variable.VarType = styp;
                            }
                            else
                            {
                                lhs[^1].Lhs.Variable.VarType = styp;
                            }
                            if (token.Tok != Token.Semi && !Got(Token.Comma))
                            {
                                Error($"unexpected {token.Tok} in list; expected comma or end of list");
                                break;
                            }
                        }
                        stmt.Type = StmtType.BlockDeclaration;
                        stmt.LhsTup = lhs;
                    }
                    break;
                case Token.Component:
                    Next();
                    if (Got(Token.Lparen))
                    {
                        List<SourceExpression> lhs = new List<SourceExpression>();
                        while (!Got(Token.Rparen))
                        {
                            lhs.Add(new SourceExpression { Op = Op.Variable, Variable = ParseSimpleSymbol() });
                            lhs[^1].Variable.VarType = VarType.Component;
                            if (token.Tok != Token.Rparen && !Got(Token.Comma))
                            {
                                Error($"unexpected {token.Tok} in list; expected comma or end of list");
                                break;
                            }
                        }
                        stmt.Type = StmtType.BlockDeclaration;
                        stmt.LhsTup = lhs;
                        if (GotAssignOp(out var _op))
                        {
                            stmt.Rhs = ParseExpr();
                            stmt.AssignOp = _op;
                            stmt.Type = StmtType.InitializationBlock;
                        }
                    }
                    else
                    {
                        List<SourceExpression> lhs = new List<SourceExpression>();
                        while (token.Tok != Token.Semi)
                        {
                            lhs.Add(ParseSymbol());
                            if (lhs[^1].Op == Op.Variable)
                            {
                                lhs[^1].Variable.VarType = VarType.Component;
                            }
                            else
                            {
                                lhs[^1].Lhs.Variable.VarType = VarType.Component;
                            }
                            if (token.Tok != Token.Semi && !Got(Token.Comma))
                            {
                                Error($"unexpected {token.Tok} in list; expected comma or end of list");
                                break;
                            }
                        }
                        stmt.Type = StmtType.BlockDeclaration;
                        stmt.LhsTup = lhs;
                    }
                    break;
                default:
                    Error($"unexpected {token.Tok} in declaration");
                    break;
            }

            return stmt;
        }

        protected SourceStmt ParseSubstitution(bool allowSimpleStmt = true, bool allowConstraint = false)
        {
            return ParseSubstitutionOrSimple(allowSimpleStmt, allowConstraint);
        }

        protected SourceStmt ParseSubstitutionOrSimple(bool allowSimpleStmt = true, bool allowConstraint = false)
        {
            var stmt = new SourceStmt();
            stmt.Lhs = ParseExpr();
            stmt.Type = StmtType.Substitution;
            switch (token.Tok)
            {
                case Token.Assign:
                    Next();
                    stmt.AssignOp = AssignOp.VarAssign;
                    stmt.Rhs = ParseExpr();
                    break;
                case Token.LeftAssign:
                    Next();
                    stmt.AssignOp = AssignOp.SignalAssign;
                    stmt.Rhs = ParseExpr();
                    break;
                case Token.LeftConstrain:
                    Next();
                    stmt.AssignOp = AssignOp.ConstrainAssign;
                    stmt.Rhs = ParseExpr();
                    break;
                case Token.RightAssign:
                    Next();
                    stmt.AssignOp = AssignOp.SignalAssign;
                    var rAsw = ParseExpr();
                    stmt.Rhs = stmt.Lhs;
                    stmt.Lhs = rAsw;
                    break;
                case Token.RightConstrain:
                    Next();
                    stmt.AssignOp = AssignOp.ConstrainAssign;
                    var rCsw = ParseExpr();
                    stmt.Rhs = stmt.Lhs;
                    stmt.Lhs = rCsw;
                    break;
                case Token.Constrain:
                    Next();
                    if (allowConstraint)
                    {
                        stmt.Type = StmtType.Constrain;
                        stmt.Rhs = ParseExpr();
                    }
                    else
                    {
                        Error("unexpected constrain statement");
                        return stmt;
                    }
                    break;
                case Token.Semi:
                    if (allowSimpleStmt)
                    {
                        Error("expected substution; got expression statement");
                    }
                    return stmt;
                default:
                    return ParseAssignOpSubstitution(stmt);
            }

            return stmt;
        }

        protected SourceStmt ParseAssignOpSubstitution(SourceStmt stmt)
        {
            if (stmt.Lhs.Op != Op.Variable)
            {
                Error($"Expected variable on left hand side of operation assignment");
                return stmt;
            }

            var op = Op.Undefined;
            switch (token.Tok)
            {
                case Token.AddAsgn:
                    op = Op.Add; break;
                case Token.SubAsgn:
                    op = Op.Sub; break;
                case Token.MulAsgn:
                    op = Op.Mul; break;
                case Token.DivAsgn:
                    op = Op.Div; break;
                case Token.IDivAsgn:
                    op = Op.IntDiv; break;
                case Token.PowAsgn:
                    op = Op.Pow; break;
                case Token.ModAsgn:
                    op = Op.Mod; break;
                case Token.AndAsgn:
                    op = Op.BitAnd; break;
                case Token.OrAsgn:
                    op = Op.BitOr; break;
                case Token.XorAsgn:
                    op = Op.BitXor; break;
                case Token.NotAsgn:
                    op = Op.BitNot; break;
                case Token.ShlAsgn:
                    op = Op.ShiftL; break;
                case Token.ShrAsgn:
                    op = Op.ShiftR; break;
                case Token.Inc:
                    op = Op.Inc; break;
                case Token.Dec:
                    op = Op.Dec; break;
                default:
                    Error($"unexpected {token.Tok} in operation assignment");
                    break;
            }

            Next(); // eat operator 
            stmt.AssignOp = AssignOp.VarAssign;
            stmt.Type = StmtType.Substitution;

            switch (op)
            {
                case Op.Inc:
                    stmt.Rhs = new SourceExpression { Lhs = stmt.Lhs, Rhs = new SourceExpression { Op = Op.Literal, Literal = new Literal { Value = 1 } }, Op = Op.Add };
                    break;
                case Op.Dec:
                    stmt.Rhs = new SourceExpression { Lhs = stmt.Lhs, Rhs = new SourceExpression { Op = Op.Literal, Literal = new Literal { Value = 1 } }, Op = Op.Add };
                    break;
                case Op.Undefined:
                    break; // we already complained about this earlier
                default:
                    stmt.Rhs = new SourceExpression { Lhs = stmt.Lhs, Rhs = ParseExpr(), Op = op };
                    break;
            }

            return stmt;
        }

        protected SourceStmt ParseBlockStmt()
        {
            List<SourceStmt> stmts = new List<SourceStmt>();
            Want(Token.Lbrace);
            while (token.Tok != Token.Rbrace)
            {
                stmts.Add(ParseStmtLvl3());
            }
            Want(Token.Rbrace);

            return new SourceStmt { Type = StmtType.Block, Block = stmts };
        }

        protected SourceStmt ParseStmt()
        {
            return ParseStmtLvl0();
        }

        protected SourceStmt ParseStmtLvl0()
        {
            SourceStmt stmt = new SourceStmt();
            switch (token.Tok)
            {
                case Token.If:
                    Next();
                    Want(Token.Lparen);
                    stmt.Type = StmtType.Cond;
                    stmt.Lhs = ParseExpr();
                    Want(Token.Rparen);
                    stmt.Stmt1 = ParseStmtLvl0();
                    if (Got(Token.Else))
                    {
                        stmt.Stmt2 = ParseStmtLvl0();
                    }
                    break;
                default:
                    return ParseStmtLvl1();
            }

            return stmt;
        }

        protected SourceStmt ParseStmtLvl1()
        {
            return ParseStmtLvl2();
        }

        protected SourceStmt ParseStmtLvl2()
        {
            SourceStmt stmt = new SourceStmt();
            switch (token.Tok)
            {
                case Token.For:
                    Next();
                    Want(Token.Lparen);
                    stmt.Type = StmtType.For;
                    stmt.Stmt1 = ParseDeclarationOrSubstitution(false);
                    Want(Token.Semi);
                    stmt.Lhs = ParseExpr();
                    Want(Token.Semi);
                    stmt.Stmt2 = ParseSubstitution(false);
                    Want(Token.Rparen);
                    stmt.Body = ParseStmtLvl2();
                    break;
                case Token.While:
                    Next();
                    Want(Token.Lparen);
                    stmt.Type = StmtType.While;
                    stmt.Lhs = ParseExpr();
                    Want(Token.Rparen);
                    stmt.Body = ParseStmtLvl2();
                    break;
                case Token.Return:
                    Next();
                    stmt.Type = StmtType.Return;
                    stmt.Lhs = ParseExpr();
                    Want(Token.Semi);
                    break;
                case Token.Assert:
                    Next();
                    stmt.Type = StmtType.Assert;
                    Want(Token.Lparen);
                    stmt.Lhs = ParseExpr();
                    Want(Token.Rparen);
                    Want(Token.Semi);
                    break;
                case Token.Log:
                    Next();
                    stmt.Type = StmtType.Log;
                    Want(Token.Lparen);
                    stmt.LhsTup = new List<SourceExpression> { };
                    while (!Got(Token.Rparen))
                    {
                        if (token.Tok == Token.String)
                        {
                            stmt.LhsTup.Add(new SourceExpression { Op = Op.Literal, Literal = new Literal { String = token.Val } });
                        }
                        else
                        {
                            stmt.LhsTup.Add(ParseExpr());
                        }
                    }
                    break;
                case Token.Lbrace:
                    stmt = ParseBlockStmt();
                    break;
                default:
                    stmt = ParseSubstitution(true, true);
                    Want(Token.Semi);
                    break;
            }

            return stmt;
        }

        protected SourceStmt ParseStmtLvl3()
        {
            if (IsDeclaration())
            {
                var rv = ParseDeclaration();
                Want(Token.Semi);
                return rv;
            }

            return ParseStmt();
        }

        protected SourceExpression ParseExpr()
        {
            return ParseExpr13();
        }

        protected SourceExpression ParseExpr13()
        {
            SourceExpression expr = ParseExpr12();
            if (Got(Token.Qmark))
            {
                expr = new SourceExpression { Op = Op.Ternary, Cond = expr };
                expr.Lhs = ParseExpr12();
                expr.Rhs = ParseExpr12();
            }

            return expr;
        }

        protected SourceExpression ParseExpr12()
        {
            SourceExpression expr = ParseExpr11();
            if (Got(Token.BoolOr))
            {
                expr = new SourceExpression { Op = Op.Or, Lhs = expr, Rhs = ParseExpr12() };
            }

            return expr;
        }

        protected SourceExpression ParseExpr11()
        {
            SourceExpression expr = ParseExpr10();
            if (Got(Token.BoolAnd))
            {
                expr = new SourceExpression { Op = Op.And, Lhs = expr, Rhs = ParseExpr11() };
            }

            return expr;
        }

        protected SourceExpression ParseExpr10()
        {
            SourceExpression expr = ParseExpr9();
            if (Got(Token.Geq))
            {
                expr = new SourceExpression { Op = Op.GreaterEq, Lhs = expr, Rhs = ParseExpr10() };
            }
            else if (Got(Token.Gtr))
            {
                expr = new SourceExpression { Op = Op.Greater, Lhs = expr, Rhs = ParseExpr10() };
            }
            else if (Got(Token.Leq))
            {
                expr = new SourceExpression { Op = Op.LesserEq, Lhs = expr, Rhs = ParseExpr10() };
            }
            else if (Got(Token.Lss))
            {
                expr = new SourceExpression { Op = Op.Lesser, Lhs = expr, Rhs = ParseExpr10() };
            }
            else if (Got(Token.Eq))
            {
                expr = new SourceExpression { Op = Op.Eq, Lhs = expr, Rhs = ParseExpr10() };
            }
            else if (Got(Token.Neq))
            {
                expr = new SourceExpression { Op = Op.Neq, Lhs = expr, Rhs = ParseExpr10() };
            }

            return expr;
        }

        protected SourceExpression ParseExpr9()
        {
            SourceExpression expr = ParseExpr8();
            if (Got(Token.BitOr))
            {
                expr = new SourceExpression { Op = Op.BitOr, Lhs = expr, Rhs = ParseExpr9() };
            }

            return expr;
        }

        protected SourceExpression ParseExpr8()
        {
            SourceExpression expr = ParseExpr7();
            if (Got(Token.BitXor))
            {
                expr = new SourceExpression { Op = Op.BitXor, Lhs = expr, Rhs = ParseExpr8() };
            }

            return expr;
        }

        protected SourceExpression ParseExpr7()
        {
            SourceExpression expr = ParseExpr6();
            if (Got(Token.BitAnd))
            {
                expr = new SourceExpression { Op = Op.BitAnd, Lhs = expr, Rhs = ParseExpr7() };
            }

            return expr;
        }

        protected SourceExpression ParseExpr6()
        {
            SourceExpression expr = ParseExpr5();
            if (Got(Token.BitShl))
            {
                expr = new SourceExpression { Op = Op.ShiftL, Lhs = expr, Rhs = ParseExpr6() };
            }
            else if (Got(Token.BitShr))
            {
                expr = new SourceExpression { Op = Op.ShiftR, Lhs = expr, Rhs = ParseExpr6() };
            }

            return expr;
        }

        protected SourceExpression ParseExpr5()
        {
            SourceExpression expr = ParseExpr4();
            if (Got(Token.Add))
            {
                expr = new SourceExpression { Op = Op.Add, Lhs = expr, Rhs = ParseExpr5() };
            }
            else if (Got(Token.Sub))
            {
                expr = new SourceExpression { Op = Op.Sub, Lhs = expr, Rhs = ParseExpr5() };
            }

            return expr;
        }

        protected SourceExpression ParseExpr4()
        {
            SourceExpression expr = ParseExpr3();
            if (Got(Token.Mul))
            {
                expr = new SourceExpression { Op = Op.Mul, Lhs = expr, Rhs = ParseExpr4() };
            }
            else if (Got(Token.Div))
            {
                expr = new SourceExpression { Op = Op.Div, Lhs = expr, Rhs = ParseExpr4() };
            }
            else if (Got(Token.IDiv))
            {
                expr = new SourceExpression { Op = Op.IntDiv, Lhs = expr, Rhs = ParseExpr4() };
            }
            else if (Got(Token.Mod))
            {
                expr = new SourceExpression { Op = Op.Mod, Lhs = expr, Rhs = ParseExpr4() };
            }

            return expr;
        }

        protected SourceExpression ParseExpr3()
        {
            SourceExpression expr = ParseExpr2();
            if (Got(Token.Pow))
            {
                expr = new SourceExpression { Op = Op.Pow, Lhs = expr, Rhs = ParseExpr3() };
            }

            return expr;
        }

        protected SourceExpression ParseExpr2()
        {
            switch (token.Tok)
            {
                case Token.Not:
                    Next();
                    return new SourceExpression { Op = Op.Not, Lhs = ParseExpr1() };
                case Token.Sub:
                    Next();
                    return new SourceExpression { Op = Op.Neg, Lhs = ParseExpr1() };
                case Token.BitNot:
                    Next();
                    return new SourceExpression { Op = Op.BitNot, Lhs = ParseExpr1() };
                default:
                    return ParseExpr1();
            }
        }

        protected SourceExpression ParseExpr1()
        {
            SourceExpression expr = new SourceExpression();
            switch (token.Tok)
            {
                case Token.Identifier:
                    var name = ParseName();
                    if (Got(Token.Lparen))
                    {
                        expr.Expressions = new();
                        expr.Op = Op.FuncCall;
                        expr.Variable = new Variable { Name = name, VarType = VarType.Function };
                        // function call or anonymous component
                        while (!Got(Token.Rparen))
                        {
                            expr.Expressions.Add(ParseExpr());
                            if (token.Tok != Token.Comma && token.Tok != Token.Rparen)
                            {
                                Error($"unexpected {token.Tok} in argument list");
                                return expr;
                            }
                            if (token.Tok == Token.Comma) Next();
                        }

                        if (Got(Token.Lparen))
                        {
                            expr.Op = Op.AnonymousComp;
                            expr.AnonArgs = new();
                            expr.Variable.VarType = VarType.AnonymousComponent;
                            bool expectListableWithNames = false;
                            if (token.Tok == Token.Identifier)
                            {
                                var ntok = Peek();
                                if (ntok.Tok == Token.Assign || ntok.Tok == Token.LeftAssign || ntok.Tok == Token.LeftConstrain)
                                {
                                    // we have a listable with input names
                                    expectListableWithNames = true;
                                }
                            }
                            while (!Got(Token.Rparen))
                            {
                                if (expectListableWithNames)
                                {
                                    var nm = ParseName();
                                    if (!GotAssignOp(out var asgnop))
                                    {
                                        Error("expected an assignment operator in anonymous component arguments with at least one named assignment");
                                    }
                                    var aexpr = ParseExpr();
                                    expr.AnonArgs.Add((nm, asgnop, aexpr));
                                }
                                else
                                {
                                    expr.AnonArgs.Add(("", AssignOp.VarAssign, ParseExpr()));
                                }
                                if (token.Tok != Token.Comma && token.Tok != Token.Rparen)
                                {
                                    Error($"unexpected {token.Tok} in argument list");
                                    return expr;
                                }
                                if (token.Tok == Token.Comma) Next();
                            }
                        }
                    }
                    else
                    {
                        var v = new Variable { Name = name };
                        v.Access = ParseAccessList();
                        expr = new SourceExpression { Variable = v, Op = Op.Variable };
                    }
                    break;
                case Token.Lbrack:
                    Next();
                    expr.Op = Op.Array;
                    expr.Expressions = new()
                    {
                        ParseExpr()
                    };
                    while (!Got(Token.Rbrack))
                    {
                        expr.Expressions.Add(ParseExpr());
                        if (token.Tok != Token.Comma && token.Tok != Token.Rbrack)
                        {
                            Error($"unexpected {token.Tok} in array literal");
                            return expr;
                        }
                        if (token.Tok == Token.Comma) Next();
                    }
                    break;
                case Token.Literal:
                    expr.Op = Op.Literal;
                    expr.Literal = new Literal { Value = BigInteger.Parse(token.Val) };
                    Next();
                    break;
                case Token.Lparen:
                    Next();
                    expr = ParseExpr();
                    Want(Token.Rparen);
                    break;
                default:
                    Error($"unexpected {token.Tok} in expression");
                    break;
            }

            return expr;
        }
    }
}
