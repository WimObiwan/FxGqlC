using System;

namespace FxGqlLib
{
	public abstract class WarningException : Exception
	{
		protected WarningException (string msg) : base (msg)
		{}
	}
}

