using System;

namespace FxGqlLib
{
	public class GetCurDirFunction : Expression<DataString>
	{
		public GetCurDirFunction ()
		{
		}
				
		#region implemented abstract members of FxGqlLib.Expression[System.String]
		public override DataString Evaluate (GqlQueryState gqlQueryState)
		{
			return gqlQueryState.CurrentDirectory;
		}
		#endregion
	}
}

