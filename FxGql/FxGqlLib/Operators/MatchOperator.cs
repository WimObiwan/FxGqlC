using System;
using System.Text.RegularExpressions;
using System.Collections;

namespace FxGqlLib
{
	public class MatchOperator : Expression<DataBoolean>
	{
		readonly IExpression arg1;
		readonly IExpression arg2;
		readonly RegexOptions regexOptions;
		
		public MatchOperator (IExpression arg1, IExpression arg2, bool caseInsensitive)
		{
			this.arg1 = arg1;
			this.arg2 = arg2;
			if (caseInsensitive)
				regexOptions = RegexOptions.IgnoreCase;
		}

		#region implemented abstract members of FxGqlLib.Expression[System.String]
		public override DataBoolean Evaluate (GqlQueryState gqlQueryState)
		{
			return Regex.IsMatch (arg1.EvaluateAsData (gqlQueryState).ToDataString (), arg2.EvaluateAsData (gqlQueryState).ToDataString (), regexOptions);
		}
		#endregion

		public override bool IsConstant ()
		{
			// TODO: Optimization: Too strong: if no match: arg3 doesn't need to be constant
			return arg1.IsConstant () && arg2.IsConstant ();
		}
	}
}

