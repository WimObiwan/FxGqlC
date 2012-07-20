using System;
using System.Text.RegularExpressions;

namespace FxGqlLib
{
	public class MatchRegexFunction : Expression<DataString>
	{
		readonly IExpression arg1;
		readonly IExpression arg2;
		readonly IExpression arg3;
		readonly RegexOptions regexOptions;
		
		public MatchRegexFunction (IExpression arg1, IExpression arg2, bool caseInsensitive)
			: this(arg1, arg2, caseInsensitive, null)
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
		public override DataString Evaluate (GqlQueryState gqlQueryState)
		{
			Match match = Regex.Match (arg1.EvaluateAsData (gqlQueryState).ToDataString (), arg2.EvaluateAsData (gqlQueryState).ToDataString (), regexOptions);
			if (match.Success) {
				if (arg3 != null) {
					return match.Result (arg3.EvaluateAsData (gqlQueryState).ToDataString ());
				} else if (match.Groups.Count > 1) {
					return match.Groups [1].Value;
				} else {
					return match.Groups [0].Value;
				}
			} else {
				if (gqlQueryState != null) {
					gqlQueryState.SkipLine = true;
				}
				return "";
			}
		}
		#endregion
	}
}

