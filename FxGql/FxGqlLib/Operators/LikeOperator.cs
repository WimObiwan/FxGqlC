using System;
using System.Text.RegularExpressions;
using System.Globalization;

namespace FxGqlLib
{
	public class LikeOperator : MatchOperator
	{
		static readonly ConstExpression<DataString> PREFIX = new ConstExpression<DataString> ("^");
		static readonly ConstExpression<DataString> SUFFIX = new ConstExpression<DataString> ("$");

		public LikeOperator (IExpression arg1, IExpression arg2, bool caseInsensitive, CultureInfo cultureInfo)
			: base (arg1, ConstructLikeExpression (arg2), caseInsensitive, cultureInfo)
		{
		}

		static Expression<DataString> ConstructLikeExpression (IExpression expression)
		{
			expression = UnaryExpression<DataString, DataString>.CreateAutoConvert ((a) => Regex.Escape (a), expression, CultureInfo.InvariantCulture);
			expression = new ReplaceFunction (expression, new ConstExpression<DataString> ("%"), new ConstExpression<DataString> (".*"), false, CultureInfo.InvariantCulture);
			expression = new ReplaceFunction (expression, new ConstExpression<DataString> ("_"), new ConstExpression<DataString> ("."), false, CultureInfo.InvariantCulture);
			expression = BinaryExpression<DataString, DataString, DataString>.CreateAutoConvert ((a, b) => a + b, PREFIX, expression, CultureInfo.InvariantCulture); 
			return BinaryExpression<DataString, DataString, DataString>.CreateAutoConvert ((a, b) => a + b, expression, SUFFIX, CultureInfo.InvariantCulture); 
		}
	}
}

