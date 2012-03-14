using System;

namespace FxGqlLib
{
	public class LineNoSystemVar : Expression<long>
	{
		public LineNoSystemVar ()
		{
		}

		#region implemented abstract members of FxGqlLib.Expression[System.Int64]
		public override long Evaluate (GqlQueryState gqlQueryState)
		{
			return gqlQueryState.Record.LineNo;
		}
		#endregion
	}
}

