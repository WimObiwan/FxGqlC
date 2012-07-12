using System;

namespace FxGqlLib
{
	public class LineNoSystemVar : Expression<DataInteger>
	{
		public LineNoSystemVar ()
		{
		}

		#region implemented abstract members of FxGqlLib.Expression[System.Int64]
		public override DataInteger Evaluate (GqlQueryState gqlQueryState)
		{
			return gqlQueryState.Record.LineNo;
		}
		#endregion
	}
}

