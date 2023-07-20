using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoLib2
{
    internal class R1CS
    {
        public List<R1CSLine> lines;

        public R1CS() { }

        public override string ToString()
        {
            StringBuilder rv = new StringBuilder();
            for (int i = 0; i < lines.Count; i++)
            {
                rv.Append($"operation {i + 1}\n");
                rv.Append(lines[i].ToString());
            }

            return rv.ToString(); 
        }

        public string ToString2()
        {
            StringBuilder rv = new StringBuilder();
            for (int i = 0; i < lines.Count; i++)
            {
                rv.Append($"operation {i + 1}\n");
                rv.Append(lines[i].ToString2());
            }

            return rv.ToString();
        }

        public string ToStringBits()
        {
            StringBuilder rv = new StringBuilder();
            for (int i = 0; i < lines.Count; i++)
            {
                rv.Append($"operation {i + 1}\n");
                rv.Append(lines[i].ToStringBits());
            }

            return rv.ToString();
        }

        public R1CS(SimpleProgram p)
        {
            int n = p.Program.Count;

            var symbols = p.GetWitness();

            // compile lines
            lines = new List<R1CSLine>();
            for (int i = 0; i < n; i++)
            {
                lines.Add(new R1CSLine(symbols, p.Program[i]));
            }
            //lines = new List<R1CSLine>(n);
        }

        public R1CS Simplify()
        {
            // simplify converts all negative coefficients to their respective additive inverse under Z/pZ
            for (int i = 0; i < lines.Count; i++)
            {
                lines[i].Simplify();
            }

            return this;
        }
    }
}
