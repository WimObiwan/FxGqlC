using System;
using System.Text.RegularExpressions;
using System.Collections;
using System.Globalization;

namespace FxGqlLib
{
	public class MatchOperator : Expression<DataBoolean>
	{
		readonly IExpression arg1;
		readonly IExpression arg2;
		readonly RegexOptions regexOptions;
		readonly CultureInfo cultureInfo;
		
		public MatchOperator (IExpression arg1, IExpression arg2, bool caseInsensitive, CultureInfo cultureInfo)
		{
			this.arg1 = arg1;
			this.arg2 = arg2;
			this.cultureInfo = cultureInfo;
			this.regexOptions = RegexOptions.None;
			if (cultureInfo.LCID == CultureInfo.InvariantCulture.LCID)
				regexOptions |= RegexOptions.CultureInvariant;
			if (caseInsensitive)
				regexOptions |= RegexOptions.IgnoreCase;
		}

		#region implemented abstract members of FxGqlLib.Expression[System.String]
		public override DataBoolean Evaluate (GqlQueryState gqlQueryState)
		{
			return Regex.IsMatch (arg1.EvaluateAsData (gqlQueryState).ToDataString (cultureInfo), arg2.EvaluateAsData (gqlQueryState).ToDataString (cultureInfo), regexOptions);
		}
		#endregion

		public override bool IsConstant ()
		{
			// TODO: Optimization: Too strong: if no match: arg3 doesn't need to be constant
			return arg1.IsConstant () && arg2.IsConstant ();
		}
	}
}

