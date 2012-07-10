using System;

namespace FxGqlLib
{
	public class ConversionException : Exception
	{
		public ConversionException (Type from, Type to)
			:base(string.Format("Conversion from {0} to {1} not allowed", from, to))
		{
		}
	}
}

