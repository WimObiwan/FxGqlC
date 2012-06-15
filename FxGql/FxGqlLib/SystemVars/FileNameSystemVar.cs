using System;
using System.IO;


namespace FxGqlLib
{
	public class FileNameSystemVar : Expression<string>
	{
		bool full;

		public FileNameSystemVar (bool full)
		{
			this.full = full;
		}

		#region implemented abstract members of FxGqlLib.Expression[System.String]
		public override string Evaluate (GqlQueryState gqlQueryState)
		{
			if (full)
				return gqlQueryState.Record.Source;
			else
				return Path.GetFileName (gqlQueryState.Record.Source);
		}
		#endregion
	}
}

