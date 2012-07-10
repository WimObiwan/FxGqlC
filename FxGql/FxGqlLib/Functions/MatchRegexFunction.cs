using System;
using System.Text.RegularExpressions;

namespace FxGqlLib
{
	public class MatchRegexFunction : Expression<string>
	{
		readonly IExpression arg1;
		readonly IExpression arg2;
		readonly IExpression arg3;
		readonly RegexOptions regexOptions;
		
		public MatchRegexFunction (IExpression arg1, IExpression arg2, bool caseInsensitive)
			: this(arg1, arg2, caseInsensitive, new ConstExpression<string>("$1"))
		{
		}
		
		public MatchRegexFunction (IExpression arg1, IExpression arg2, bool caseInsensitive, IExpression arg3)
		{
			this.arg1 = arg1;
			this.arg2 = arg2;
			this.arg3 = arg3;
			if (caseInsensitive)
				regexOptions = RegexOptions.IgnoreCase;
		}

		#region implemented abstract members of FxGqlLib.Expression[System.String]
		public override string Evaluate (GqlQueryState gqlQueryState)
		{
			Match match = Regex.Match (arg1.EvaluateAsString (gqlQueryState), arg2.EvaluateAsString (gqlQueryState), regexOptions);
			if (match.Success)
				return match.Result (arg3.EvaluateAsString (gqlQueryState));
			else
				return "";
		}
		#endregion
	}
}

