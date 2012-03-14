using System;

namespace FxGqlLib
{
	public class FormatColumnListFunction : Expression<string>
	{
		string separator;
		
		public FormatColumnListFunction (string separator)
		{
			this.separator = separator;
		}
				
		#region implemented abstract members of FxGqlLib.Expression[System.String]
		public override string Evaluate (GqlQueryState gqlQueryState)
		{
			IComparable[] columns = gqlQueryState.Record.Columns;
			string[] texts = new string[columns.Length * 2 - 1];
			for (int i = 0; i < columns.Length; i++) {
				if (i > 0)
					texts [i * 2 - 1] = separator;
				texts [i * 2] = columns [i].ToString ();
			}
			
			return string.Concat (texts);
		}
		#endregion
	}
}

