using System;
using System.Text.RegularExpressions;

namespace FxGqlLib
{
	public class ReplaceRegexFunction : Expression<DataString>
	{
		readonly IExpression arg1;
		readonly IExpression arg2;
		readonly IExpression arg3;
		readonly RegexOptions regexOptions;
		
		public ReplaceRegexFunction (IExpression arg1, IExpression arg2, IExpression arg3, bool caseInsensitive)
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
			return Regex.Replace (arg1.EvaluateAsData (gqlQueryState).ToDataString (), arg2.EvaluateAsData (gqlQueryState).ToDataString (), 
				arg3.EvaluateAsData (gqlQueryState).ToDataString (), regexOptions);
		}
		#endregion
	}
}

