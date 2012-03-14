using System;

namespace FxGqlLib
{
	public class LineSystemVar : Expression<string>
	{
		public LineSystemVar ()
		{
		}

		#region implemented abstract members of FxGqlLib.Expression[System.String]
		public override string Evaluate (GqlQueryState gqlQueryState)
		{
			if (gqlQueryState.UseOriginalColumns)
				return gqlQueryState.Record.OriginalColumns [0].ToString ();
			else
				return gqlQueryState.Record.Columns [0].ToString ();
		}
		#endregion
	}
}

