using System;
using System.Linq;

namespace FxGqlLib
{
	public class FormatColumnListFunction : Expression<string>
	{
		readonly string separator;
		
		public FormatColumnListFunction (string separator)
		{
			this.separator = separator;
		}
				
		#region implemented abstract members of FxGqlLib.Expression[System.String]
		public override string Evaluate (GqlQueryState gqlQueryState)
		{
			string[] columns = gqlQueryState.Record.Columns.Select (p => p.ToString ()).ToArray ();
			return Evaluate (columns);
		}
		#endregion

		public string Evaluate (string[] columns)
		{
			string[] texts = new string[columns.Length * 2 - 1];
			for (int i = 0; i < columns.Length; i++) {
				if (i > 0)
					texts [i * 2 - 1] = separator;
				texts [i * 2] = columns [i];
			}
			
			return string.Concat (texts);
		}
	}
}

