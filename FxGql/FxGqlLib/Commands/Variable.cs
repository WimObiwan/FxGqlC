using System;

namespace FxGqlLib
{
	public class Variable
	{
		public string Name { get; set; }
		public Type Type { get; set; }
		public IData Value { get; set; }
	}
}

