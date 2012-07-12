using System;

namespace FxGqlLib
{
	public class TotalLineNoSystemVar : Expression<DataInteger>
	{
		public TotalLineNoSystemVar ()
		{
		}

		#region implemented abstract members of FxGqlLib.Expression[System.Int64]
		public override DataInteger Evaluate (GqlQueryState gqlQueryState)
		{
			return gqlQueryState.Record.TotalLineNo;
		}
		#endregion
	}
}

