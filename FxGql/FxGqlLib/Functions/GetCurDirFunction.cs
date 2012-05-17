using System;

namespace FxGqlLib
{
	public class GetCurDirFunction : Expression<string>
	{
		public GetCurDirFunction ()
		{
		}
				
		#region implemented abstract members of FxGqlLib.Expression[System.String]
		public override string Evaluate (GqlQueryState gqlQueryState)
		{
			return gqlQueryState.CurrentDirectory;
		}
		#endregion
	}
}

