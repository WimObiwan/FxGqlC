using System;

namespace FxGqlLib
{
	public abstract class WarningException : Exception
	{
		protected WarningException (string msg) : base (msg)
		{
		}

		protected WarningException (string msg, Exception inner) : base (msg, inner)
		{
		}
	}
}

