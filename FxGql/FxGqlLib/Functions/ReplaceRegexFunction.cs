using System;
using System.Text.RegularExpressions;

namespace FxGqlLib
{
	public class ReplaceRegexFunction : Expression<DataString>
	{
		readonly IExpression origin;
		readonly IExpression regex;
		readonly IExpression replace;
		readonly RegexOptions regexOptions;

		readonly Regex regex2;

		public ReplaceRegexFunction (IExpression origin, IExpression regex, IExpression replace, bool caseInsensitive)
		{
			this.origin = origin;
			this.regex = regex;
			this.replace = replace;
			this.regexOptions = RegexOptions.CultureInvariant;
			if (caseInsensitive)
				regexOptions = RegexOptions.IgnoreCase;

			if (regex.IsConstant ())
				regex2 = new Regex (regex.EvaluateAsData (null).ToDataString (), regexOptions);
		}

		#region implemented abstract members of FxGqlLib.Expression[System.String]
		public override DataString Evaluate (GqlQueryState gqlQueryState)
		{
			string input = origin.EvaluateAsData (gqlQueryState).ToDataString ();
			if (regex2 != null)
				return regex2.Replace (input, replace.EvaluateAsData (gqlQueryState).ToDataString ());
			else
				return Regex.Replace (input, regex.EvaluateAsData (gqlQueryState).ToDataString (), 
					replace.EvaluateAsData (gqlQueryState).ToDataString (), regexOptions);
		}
		#endregion

		public override bool IsConstant ()
		{
			// TODO: Optimization: Too strong: if no match: arg3 doesn't need to be constant
			return origin.IsConstant () && regex.IsConstant () && replace.IsConstant ();
		}
	}
}

