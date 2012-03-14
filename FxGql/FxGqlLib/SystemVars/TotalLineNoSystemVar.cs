using System;

namespace FxGqlLib
{
	public class TotalLineNoSystemVar : Expression<long>
	{
		public TotalLineNoSystemVar ()
		{
		}

		#region implemented abstract members of FxGqlLib.Expression[System.Int64]
		public override long Evaluate (GqlQueryState gqlQueryState)
		{
			return gqlQueryState.TotalLineNumber;
		}
		#endregion
	}
}

