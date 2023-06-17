using AuremCore.Crypto.BN256;
using AuremCore.Crypto.BN256.Native;
using AuremCore.Crypto.P2P;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Testing
{
    internal class SimpleProgram
    {
        public List<Operation> Program;
        public List<Symbol> Inputs;
        public List<Symbol> Outputs;
        public List<Symbol> Hiddens;

        public SimpleProgram() { }

        public Symbol[] GetWitness()
        {
            int wn = Inputs.Count + Outputs.Count + Hiddens.Count + 1;

            // make one
            Symbol[] syms = new Symbol[wn];
            syms[0] = Symbol.Make("one");
            syms[0].constant = true;
            syms[0].value = BigInteger.One;

            // copy elements to w
            int isym = 1;
            Array.Copy(Outputs.ToArray(), 0, syms, isym, Outputs.Count);
            isym += Outputs.Count;
            Array.Copy(Inputs.ToArray(), 0, syms, isym, Inputs.Count);
            isym += Inputs.Count;
            Array.Copy(Hiddens.ToArray(), 0, syms, isym, Hiddens.Count);

            // remove constants from w
            List<Symbol> symbolsList = new List<Symbol>();
            symbolsList.Add(syms[0]);
            for (int i = 1; i < wn; i++)
            {
                if (syms[i].constant)
                {
                    continue;
                }

                symbolsList.Add(syms[i]);
            }

            // compile the symbols to a w offset vector
            var symbols = symbolsList.ToArray();
            for (int i = 0; i < symbols.Length; i++) symbols[i].offset = i;

            return symbols;
        }

        public static SimpleProgram Create()
        {
            Symbol x = Symbol.MakeInput("x");
            Symbol y = Symbol.Make("y");
            Symbol sym1 = Symbol.Make("sym1");
            Symbol sym2 = Symbol.Make("sym2");
            //Symbol sym3 = Symbol.Make("sym3");
            Symbol _out = Symbol.Make("out");
            Symbol five = Symbol.Make("five");
            five.value = new BigInteger(5);
            five.constant = true;

            List<Operation> ops = new List<Operation>
            {
                new Operation(sym1, x, x, Op.MUL),
                new Operation(y, sym1, x, Op.MUL),
                new Operation(sym2, y, x, Op.ADD),
                //new Operation(sym3, x, x, Op.ADD),
                new Operation(_out, sym2, five, Op.ADD)
            };

            List<Symbol> inputs = new List<Symbol>
            {
                x
            };

            List<Symbol> outputs = new List<Symbol>
            {
                _out,
                //sym3,
            };

            List<Symbol> hiddens = new List<Symbol> {sym1, y, sym2 };

            return new SimpleProgram { Hiddens = hiddens, Inputs = inputs, Outputs = outputs, Program = ops };
        }

        public BigInteger[] Execute(List<BigInteger> inputs)
        {
            if (inputs.Count != Inputs.Count) throw new ArgumentException();

            // we can compute w
            var sw = GetWitness();
            var w = new BigInteger[sw.Length];
            w[0] = sw[0].value; // 1
            for (int i = 1; i < sw.Length; i++)
            {
                // pre-initialize to zero to be sure
                w[i] = BigInteger.Zero;
            }

            for (int i = 1 + Outputs.Count; i < inputs.Count + 1 + Outputs.Count; i++)
            {
                w[i] = inputs[i - 1 - Outputs.Count];
            }

            for (int i = 0; i < Program.Count; i++)
            {
                BigInteger tmp1 = (Program[i].rhs1.constant) ? Program[i].rhs1.value : w[Program[i].rhs1.offset];
                BigInteger tmp2 = 0;
                if (Program[i].rhs2 != null) tmp2 = (Program[i].rhs2.constant) ? Program[i].rhs2.value : w[Program[i].rhs2.offset];

                switch (Program[i].op)
                {
                    case Op.ADD:
                        w[Program[i].lhs.offset] = tmp1 + tmp2;
                        break;
                    case Op.SUB:
                        w[Program[i].lhs.offset] = tmp1 - tmp2;
                        break;
                    case Op.MUL:
                        w[Program[i].lhs.offset] = tmp1 * tmp2;
                        break;
                    case Op.ASSIGN:
                        w[Program[i].lhs.offset] = (Program[i].rhs1.constant) ? Program[i].rhs1.value : w[Program[i].rhs1.offset];
                        break;
                }
            }

            for (int i = 0; i < w.Length; i++)
            {
                w[i] %= Constants.Order;
                if (w[i] < 0) w[i] += Constants.Order;
            }

            return w;
        }
    }
}
