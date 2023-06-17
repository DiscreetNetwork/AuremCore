using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Testing
{
    internal class Symbol
    {
        public string Sym;
        public bool input = false;
        public bool output = false;
        public bool constant = false;
        public int offset = 0;

        public BigInteger value = BigInteger.Zero;

        public Symbol() { }

        public static Symbol MakeInput(string sym)
        {
            Symbol sim = new Symbol();
            sim.Sym = sym;
            sim.output = false;
            sim.input = true;
            sim.value = BigInteger.Zero;

            return sim;
        }

        public static Symbol MakeOutput(string sym)
        {
            Symbol sim = new Symbol();
            sim.Sym = sym;
            sim.output = true;
            sim.input = false;
            sim.value = BigInteger.Zero;

            return sim;
        }

        public static Symbol Make(string sym)
        {
            Symbol sim = new Symbol();
            sim.Sym = sym;
            sim.output = false;
            sim.input = false;
            sim.value = BigInteger.Zero;

            return sim;
        }

        public static Symbol MakeConstant(BigInteger c)
        {
            Symbol sim = new Symbol();
            sim.Sym = "#constant";
            sim.output = false;
            sim.input = false;
            sim.constant = true;
            sim.value = c;

            return sim;
        }
    }
}
