using System;

namespace FxGqlLib
{
	public class InterruptedException : Exception
	{
		public InterruptedException ()
			: base ("GQL command was interrupted")
		{
		}
	}
}

