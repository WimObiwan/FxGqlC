using System;

namespace FxGqlLib
{
	public class LineIgnoredException : Exception
	{
		public LineIgnoredException (Exception inner) : base ("Line ignored", inner)
		{
		}
	}
}