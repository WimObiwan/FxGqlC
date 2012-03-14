using System;

namespace FxGqlLib
{
	public class FileNameSystemVar : Expression<string>
	{
		public FileNameSystemVar ()
		{
		}

		#region implemented abstract members of FxGqlLib.Expression[System.String]
		public override string Evaluate (GqlQueryState gqlQueryState)
		{
			return gqlQueryState.Record.Source;
		}
		#endregion
	}
}

