using System;
using System.Globalization;

namespace FxGqlLib
{
	public class FormatListFunction : Expression<DataString>
	{
		readonly IExpression[] expression;
		readonly string separator;
		readonly CultureInfo cultureInfo;

		public FormatListFunction (IExpression[] expression, string separator, CultureInfo cultureInfo)
		{
			this.expression = expression;
			this.separator = separator;
			this.cultureInfo = cultureInfo;
		}

		#region implemented abstract members of FxGqlLib.Expression[System.String]

		public override DataString Evaluate (GqlQueryState gqlQueryState)
		{
			string[] texts = new string[expression.Length];
			for (int i = 0; i < expression.Length; i++) {
				if (i > 0)
					texts [i * 2 - 1] = separator;
				texts [i * 2] = expression [i].EvaluateAsData (gqlQueryState).ToDataString (cultureInfo);
			}
			
			return string.Concat (texts);
		}

		#endregion

		public override bool IsConstant ()
		{
			foreach (IExpression expr in expression)
				if (!expr.IsConstant ())
					return false;
			return true;
		}
	}
}

