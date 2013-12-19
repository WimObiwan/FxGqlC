using System;
using System.Text.RegularExpressions;
using System.Globalization;

namespace FxGqlLib
{
	public class ReplaceRegexFunction : Expression<DataString>
	{
		readonly IExpression origin;
		readonly IExpression regex;
		readonly IExpression replace;
		readonly RegexOptions regexOptions;
		readonly CultureInfo cultureInfo;
		readonly Regex regex2;

		public ReplaceRegexFunction (IExpression origin, IExpression regex, IExpression replace, bool caseInsensitive, CultureInfo cultureInfo)
		{
			this.origin = origin;
			this.regex = regex;
			this.replace = replace;
			this.cultureInfo = cultureInfo;
			this.regexOptions = RegexOptions.None;
			if (cultureInfo.LCID == CultureInfo.InvariantCulture.LCID)
				regexOptions |= RegexOptions.CultureInvariant;
			if (caseInsensitive)
				regexOptions |= RegexOptions.IgnoreCase;

			if (regex.IsConstant ())
				regex2 = new Regex (regex.EvaluateAsData (null).ToDataString (cultureInfo), regexOptions);
		}

		#region implemented abstract members of FxGqlLib.Expression[System.String]

		public override DataString Evaluate (GqlQueryState gqlQueryState)
		{
			string input = origin.EvaluateAsData (gqlQueryState).ToDataString (cultureInfo);
			if (regex2 != null)
				return regex2.Replace (input, replace.EvaluateAsData (gqlQueryState).ToDataString (cultureInfo));
			else
				return Regex.Replace (input, regex.EvaluateAsData (gqlQueryState).ToDataString (cultureInfo), 
					replace.EvaluateAsData (gqlQueryState).ToDataString (cultureInfo), regexOptions);
		}

		#endregion

		public override bool IsConstant ()
		{
			// TODO: Optimization: Too strong: if no match: arg3 doesn't need to be constant
			return origin.IsConstant () && regex.IsConstant () && replace.IsConstant ();
		}
	}
}

