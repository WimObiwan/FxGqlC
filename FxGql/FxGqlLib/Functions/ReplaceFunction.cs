using System;
using System.Text.RegularExpressions;

namespace FxGqlLib
{
	public class ReplaceFunction : Expression<DataString>
	{
		readonly IExpression arg1;
		readonly IExpression arg2;
		readonly IExpression arg3;
		readonly RegexOptions regexOptions;

		public ReplaceFunction (IExpression arg1, IExpression arg2, IExpression arg3, bool caseInsensitive)
		{
			this.arg1 = arg1;
			this.arg2 = arg2;
			this.arg3 = arg3;
			if (caseInsensitive)
				regexOptions = RegexOptions.IgnoreCase;
		}

		#region implemented abstract members of FxGqlLib.Expression[System.String]
		public override DataString Evaluate (GqlQueryState gqlQueryState)
		{
			return Regex.Replace (arg1.EvaluateAsString (gqlQueryState), arg2.EvaluateAsString (gqlQueryState), 
			                      arg3.EvaluateAsString (gqlQueryState), regexOptions);
		}
		#endregion
	}
}

