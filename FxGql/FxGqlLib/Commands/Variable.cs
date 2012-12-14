using System;

namespace FxGqlLib
{
	public class Variable
	{
		public Variable (string name, Type type)
		{
			Name = name;
			Type = type;
			NewValue = new NewData () { Type = ExpressionBridge.GetNewType (type) };
		}

		public string Name { get; private set; }
		public Type Type { get; private set; }
		public IData Value { get; set; }
		public NewData NewValue { get; private set; }
	}
}

