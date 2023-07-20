using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoLib2.Comp
{
    // maxrune is 0x0010FFFF, maxbytes = 4, error = 0xFFFD, self = 0x80
    internal class SourceReader
    {
        private int _l = 0, _c = 0;
        private string buf;
        private char ch = ' ';
        private int pos = 0;
        private int mark = 0;
        private bool eof = false;
        public CToken token;

        public int Line => _l + 1;
        public int Column => _c + 1;

        public (int Line, int Column) Pos()
        {
            return (Line, Column);
        }

        public SourceReader(string filepath)
        {
            buf = File.ReadAllText(filepath) + "\n";
        }

        public SourceReader(string content, bool path)
        {
            if (path)
            {
                buf = File.ReadAllText(content) + "\n";
            }
            else
            {
                buf = content + "\n";
            }
        }

        protected void Start()
        {
            mark = pos - (pos > 0 ? 1 : 0);
        }

        public void Error(string msg)
        {
            Console.WriteLine($"{Line}:{Column}: {msg}");
            throw new Exception();
        }

        protected void Stop()
        {
            mark = -1;
        }

        protected string Segment()
        {
            return buf.Substring(mark, pos - mark - 1);
        }

        protected void NextChar()
        {
            if (pos >= buf.Length)
            {
                pos = buf.Length;
                ch = (char)0xFF;
                eof = true;
                return;
            }

            ch = buf[pos++];
            _c++;

            if (ch == '\n')
            {
                _l++;
                _c = 0;
            }
        }

        protected static bool IsWhitespace(char ch)
        {
            return ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r';
        }

        public CToken Next()
        {
            token = _Next();
            return token;
        }

        public CToken Peek()
        {
            var pl = _l; var pc = _c; var pch = ch; var peof = eof; var ppos = pos; var pmark = mark;
            var peek = Next();

            _l = pl; _c = pc; ch = pch; eof = peof; pos = ppos; mark = pmark;
            return peek;
        }

        protected CToken _Next()
        {
            if (eof)
            {
                return new CToken(Line, Column, Token.EOF);
            }
            var val = "";
            Token tok;

        redo:
            Stop();
            val = "";

            while (!eof && IsWhitespace(ch))
            {
                NextChar();
            }

            Start();
            int bl = Line, bc = _c;
            
            switch (ch)
            {
                case (char)0:
                case (char)0xFF:
                    eof = true;
                    return new CToken(bl, bc, Token.EOF);
                case ';':
                    NextChar();
                    tok = Token.Semi;
                    break;
                case '(':
                    NextChar();
                    tok = Token.Lparen;
                    break;
                case ')':
                    NextChar();
                    tok = Token.Rparen;
                    break;
                case '[':
                    NextChar();
                    tok = Token.Lbrack;
                    break;
                case ']':
                    NextChar();
                    tok = Token.Rbrack;
                    break;
                case '{':
                    NextChar();
                    tok = Token.Lbrace;
                    break;
                case '}':
                    NextChar();
                    tok = Token.Rbrace;
                    break;
                case ',':
                    NextChar();
                    tok = Token.Comma;
                    break;
                case '.':
                    NextChar();
                    tok = Token.Period;
                    break;
                case '?':
                    NextChar();
                    tok = Token.Qmark;
                    break;
                case ':':
                    NextChar();
                    tok = Token.Colon;
                    break;
                case '+':
                    NextChar();
                    if (ch == '+')
                    {
                        NextChar();
                        tok = Token.Inc;
                    }
                    else if (ch == '=')
                    {
                        NextChar();
                        tok = Token.AddAsgn;
                        val = Segment();
                    }
                    else
                    {
                        tok = Token.Add;
                    }
                    break;
                case '-':
                    NextChar();
                    if (ch == '-')
                    {
                        NextChar();
                        if (ch == '>')
                        {
                            NextChar();
                            tok = Token.RightAssign;
                        }
                        else
                        {
                            tok = Token.Dec;
                        }
                    }
                    else if (ch == '=')
                    {
                        NextChar();
                        tok = Token.SubAsgn;
                        val = Segment();
                    }
                    else
                    {
                        tok = Token.Sub;
                    }
                    break;
                case '*':
                    NextChar();
                    if (ch == '*')
                    {
                        NextChar();
                        if (ch == '=')
                        {
                            NextChar();
                            tok = Token.PowAsgn;
                        }
                        else
                        {
                            tok = Token.Pow;
                        }
                    }
                    else if (ch == '=')
                    {
                        NextChar();
                        tok = Token.MulAsgn;
                    }
                    else
                    {
                        tok = Token.Mul;
                    }
                    break;
                case '/':
                    NextChar();
                    if (ch == '/')
                    {
                        // comment
                        NextChar();
                        while (!eof && ch != '\n')
                        {
                            NextChar();
                        }
                        goto redo;
                    }
                    else if (ch == '*')
                    {
                        NextChar();
                        while (!IsBlockCommentEnd(ch))
                        {
                            NextChar();
                        }
                        goto redo;
                    }
                    else if (ch == '=')
                    {
                        NextChar();
                        tok = Token.DivAsgn;
                    }
                    else
                    {
                        tok = Token.Div;
                    }
                    break;
                case '\\':
                    NextChar();
                    if (ch == '=')
                    {
                        NextChar();
                        tok = Token.IDivAsgn;
                    }
                    else
                    {
                        tok = Token.IDiv;
                    }
                    break;
                case '>':
                    NextChar();
                    if (ch == '>')
                    {
                        NextChar();
                        if (ch == '=')
                        {
                            NextChar();
                            tok = Token.ShrAsgn;
                        }
                        else
                        {
                            tok = Token.BitShr;
                        }
                    }
                    else if (ch == '=')
                    {
                        NextChar();
                        tok = Token.Geq;
                    }
                    else
                    {
                        tok = Token.Gtr;
                    }
                    break;
                case '<':
                    NextChar();
                    if (ch == '<')
                    {
                        NextChar();
                        if (ch == '=')
                        {
                            NextChar();
                            tok = Token.ShlAsgn;
                        }
                        else
                        {
                            tok = Token.BitShl;
                        }
                    }
                    else if (ch == '=')
                    {
                        NextChar();
                        if (ch == '=')
                        {
                            NextChar();
                            tok = Token.LeftConstrain;
                        }
                        else
                        {
                            tok = Token.Leq;
                        }
                    }
                    else if (ch == '-')
                    {
                        NextChar();
                        if (ch ==  '-')
                        {
                            NextChar();
                            tok = Token.LeftAssign;
                        }
                        else
                        {
                            tok = Token.Undefined;
                            val = Segment();
                        }
                    }
                    else
                    {
                        tok = Token.Lss;
                    }
                    break;
                case '=':
                    NextChar();
                    if (ch == '=')
                    {
                        NextChar();
                        if (ch == '=')
                        {
                            NextChar();
                            tok = Token.Constrain;
                        }
                        else if (ch == '>')
                        {
                            NextChar();
                            tok = Token.RightConstrain;
                        }
                        else
                        {
                            tok = Token.Eq;
                        }
                    }
                    else
                    {
                        tok = Token.Assign;
                    }
                    break;
                case '%':
                    NextChar();
                    if (ch == '=')
                    {
                        NextChar();
                        tok = Token.ModAsgn;
                    }
                    else
                    {
                        tok = Token.Mod;
                    }
                    break;
                case '!':
                    NextChar();
                    if (ch == '=')
                    {
                        NextChar();
                        tok = Token.Neq;
                    }
                    else
                    {
                        tok = Token.Not;
                    }
                    break;
                case '&':
                    NextChar();
                    if (ch == '&')
                    {
                        NextChar();
                        tok = Token.BoolAnd;
                    }
                    else if (ch == '=')
                    {
                        NextChar();
                        tok = Token.AndAsgn;
                    }
                    else
                    {
                        tok = Token.BitAnd;
                    }
                    break;
                case '|':
                    NextChar();
                    if (ch == '|')
                    {
                        NextChar();
                        tok = Token.BoolOr;
                    }
                    else if (ch == '=')
                    {
                        NextChar();
                        tok = Token.OrAsgn;
                    }
                    else
                    {
                        tok = Token.BitOr;
                    }
                    break;
                case '~':
                    NextChar();
                    if (ch == '=')
                    {
                        NextChar();
                        tok = Token.NotAsgn;
                    }
                    else
                    {
                        tok = Token.BitNot;
                    }
                    break;
                case '^':
                    NextChar();
                    if (ch == '=')
                    {
                        NextChar();
                        tok = Token.XorAsgn;
                    }
                    else
                    {
                        tok = Token.BitXor;
                    }
                    break;
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    tok = Token.Literal;
                    val = Number();
                    break;
                case '\"':
                    NextChar();
                    tok = Token.String;
                    val = GetString();
                    break;
                default:
                    if (IsIdent(ch))
                    {
                        tok = Token.Identifier;
                        val = Ident(out Token keyw);
                        if (keyw != Token.Undefined)
                        {
                            tok = keyw;
                            val = "";
                        }
                    }
                    else
                    {
                        tok = Token.Undefined;
                        Error($"unknown symbol {Segment()}");
                        goto redo;
                    }
                    break;
            }

            return new CToken(bl, bc, tok, val);
        }

        protected bool IsBlockCommentEnd(char ch)
        {
            if (eof) return true;
            if (ch == '*')
            {
                NextChar();
                if (eof) return true;
                if (ch == '/')
                {
                    NextChar();
                    return true;
                }
            }

            return false;
        }

        protected string GetString()
        {
            StringBuilder sb = new StringBuilder();
            while (!eof && ch != '\"')
            {
                if (!eof && ch == '\\')
                {
                    NextChar();
                }
                sb.Append(ch);
                NextChar();
            }

            if (eof) Error("unterminated string literal");
            NextChar();
            return sb.ToString();
        }

        protected static bool IsIdent(char ch)
        {
            return (ch == '_' || (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9'));
        }

        protected static bool IsDecimal(char ch)
        {
            return (ch >= '0' && ch <= '9');
        }

        protected string Number()
        {
            while (IsDecimal(ch) && !eof)
            {
                NextChar();
            }

            return Segment();
        }

        protected string Ident(out Token keyword)
        {
            while (!eof && IsIdent(ch))
            {
                NextChar();
            }

            keyword = GetKeyword();

            return Segment();
        }

        protected Token GetKeyword()
        {
            var s = Segment();

            if (s == "signal") return Token.Signal;
            else if (s == "input") return Token.Input;
            else if (s == "output") return Token.Output;
            else if (s == "public") return Token.Public;
            else if (s == "template") return Token.Template;
            else if (s == "component") return Token.Component;
            else if (s == "var") return Token.Var;
            else if (s == "function") return Token.Function;
            else if (s == "return") return Token.Return;
            else if (s == "if") return Token.If;
            else if (s == "else") return Token.Else;
            else if (s == "for") return Token.For;
            else if (s == "while") return Token.While;
            else if (s == "do") return Token.Do;
            else if (s == "log") return Token.Log;
            else if (s == "assert") return Token.Assert;
            else if (s == "include") return Token.Include;
            else if (s == "pragma") return Token.Pragma;

            return Token.Undefined;
        }
    }
}
