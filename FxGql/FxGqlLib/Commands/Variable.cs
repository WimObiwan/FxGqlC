using System;

namespace FxGqlLib
{
    public class Variable
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public IComparable Value { get; set; }
    }
}

