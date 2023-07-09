using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuremCore.Testing.Comp.AST
{
    public enum SignalType
    {
        Input,
        Output,
        Intermediate
    }

    internal class Signal : Symbol
    {
        public SignalType SigType;
        public int LocalId;    // id within the component/template
        public int DagLocalId; // id within the dag

        public List<int> Lengths;

        public int Size => Lengths?.Aggregate(1, (x, y) => x * y) ?? 1;
    }

    internal class Component
    {
        public string Name;
        public List<int> Lengths;
    }
}
