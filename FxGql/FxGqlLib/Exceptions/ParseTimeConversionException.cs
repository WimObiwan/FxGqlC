using System;

namespace FxGqlLib
{
	public class ParseTimeConversionException : WarningException
	{
		public ParseTimeConversionException (Type from, Type to)
			: base (string.Format ("Conversion from {0} to {1} not allowed", from, to))
		{
		}
	}
}

