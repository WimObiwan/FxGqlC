using System;
using System.Text.RegularExpressions;

namespace FxGqlLib
{
	public class MatchRegexFunction : Expression<DataString>
	{
		readonly IExpression origin;
		readonly IExpression regex;
		readonly IExpression extract;
		readonly IExpression def;
		readonly RegexOptions regexOptions;

		readonly Regex regex2;
		
		public MatchRegexFunction (IExpression origin, IExpression regex, bool caseInsensitive)
			: this(origin, regex, caseInsensitive, null)
		{
		}
		
		public MatchRegexFunction (IExpression origin, IExpression regex, bool caseInsensitive, IExpression arg3)
			: this(origin, regex, caseInsensitive, arg3, null)
		{
		}

		public MatchRegexFunction (IExpression origin, IExpression regex, bool caseInsensitive, IExpression extract, IExpression def)
		{
			this.origin = origin;
			this.regex = regex;
			this.extract = extract;
			this.def = def;
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
			Match match;
			if (regex2 != null)
				match = regex2.Match (input);
			else
				match = Regex.Match (input, regex.EvaluateAsData (gqlQueryState).ToDataString (), regexOptions);
			if (match.Success) {
				if (extract != null) {
					return match.Result (extract.EvaluateAsData (gqlQueryState).ToDataString ());
				} else if (match.Groups.Count > 1) {
					return match.Groups [1].Value;
				} else {
					return match.Groups [0].Value;
				}
			} else {
				if (def == null) {
					if (gqlQueryState != null) 
						gqlQueryState.SkipLine = true;
					return "";
				} else {
					return def.EvaluateAsData (gqlQueryState).ToDataString ();
				}
			}
		}
		#endregion

		public override bool IsConstant ()
		{
			if (!origin.IsConstant ()) 
				return false;
			if (!regex.IsConstant ()) 
				return false;
			// TODO: Optimization: Only one of the 2 below needs to be constant...
			if (extract != null && !extract.IsConstant ()) 
				return false;
			if (extract != null && !def.IsConstant ()) 
				return false;
			return true;
		}
	}
}

