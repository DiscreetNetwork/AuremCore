using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Testing
{
    internal class SimpleProgramSymbol
    {
        public string Sym;
        public bool input = false;
        public bool output = false;
        public bool constant = false;
        public int offset = 0;

        public BigInteger value = BigInteger.Zero;

        public SimpleProgramSymbol() { }

        public static SimpleProgramSymbol MakeInput(string sym)
        {
            SimpleProgramSymbol sim = new SimpleProgramSymbol();
            sim.Sym = sym;
            sim.output = false;
            sim.input = true;
            sim.value = BigInteger.Zero;

            return sim;
        }

        public static SimpleProgramSymbol MakeOutput(string sym)
        {
            SimpleProgramSymbol sim = new SimpleProgramSymbol();
            sim.Sym = sym;
            sim.output = true;
            sim.input = false;
            sim.value = BigInteger.Zero;

            return sim;
        }

        public static SimpleProgramSymbol Make(string sym)
        {
            SimpleProgramSymbol sim = new SimpleProgramSymbol();
            sim.Sym = sym;
            sim.output = false;
            sim.input = false;
            sim.value = BigInteger.Zero;

            return sim;
        }

        public static SimpleProgramSymbol MakeConstant(BigInteger c)
        {
            SimpleProgramSymbol sim = new SimpleProgramSymbol();
            sim.Sym = "#constant";
            sim.output = false;
            sim.input = false;
            sim.constant = true;
            sim.value = c;

            return sim;
        }
    }
}
