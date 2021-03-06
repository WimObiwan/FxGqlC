using System;
using System.Text.RegularExpressions;
using System.Collections;

namespace FxGqlLib
{
	public class RegexOperator : Expression<bool>
	{
		IExpression arg1;
		IExpression arg2;
		RegexOptions regexOptions;
		
		public RegexOperator (IExpression arg1, IExpression arg2, bool caseInsensitive)
		{
			this.arg1 = arg1;
			this.arg2 = arg2;
			if (caseInsensitive)
				regexOptions = RegexOptions.IgnoreCase;
		}

		#region implemented abstract members of FxGqlLib.Expression[System.String]
		public override bool Evaluate (GqlQueryState gqlQueryState)
		{
			return Regex.IsMatch (arg1.EvaluateAsString (gqlQueryState), arg2.EvaluateAsString (gqlQueryState), regexOptions);
		}
		#endregion
	}
}

