using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aurem.Linear
{
    public enum Vote: int
    {
        Popular = 0,
        Unpopular = 1,
        Undecided = 2
    }

    public static class VoteExtensions
    {
        public static int ToVal(this Vote v) => (int)v;
        public static Vote FromVal(this int v) => (Vote)v;
    }
}
