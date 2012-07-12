using System;

namespace FxGqlLib
{
	public class LineSystemVar : Expression<DataString>
	{
		public LineSystemVar ()
		{
		}

		#region implemented abstract members of FxGqlLib.Expression[System.String]
		public override DataString Evaluate (GqlQueryState gqlQueryState)
		{
			IData[] columns;
			if (gqlQueryState.UseOriginalColumns)
				columns = gqlQueryState.Record.OriginalColumns;
			else
				columns = gqlQueryState.Record.Columns;
			
			string column = "";
			for (int i = 0; i < columns.Length; i++) {
				if (i == 0)
					column = columns [i].ToString ();
				else
					column += '\t' + columns [i].ToString ();
			}
			
			return column;
		}
		#endregion
	}
}

