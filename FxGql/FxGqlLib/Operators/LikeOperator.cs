using System;
using System.Text.RegularExpressions;

namespace FxGqlLib
{
	public class LikeOperator : MatchOperator
	{
		static readonly ConstExpression<string> PREFIX = new ConstExpression<string> ("^");
		static readonly ConstExpression<string> SUFFIX = new ConstExpression<string> ("$");
		
		public LikeOperator (IExpression arg1, IExpression arg2, bool caseInsensitive)
			: base(arg1, ConstructLikeExpression(arg2), caseInsensitive)
		{
		}
		
		static Expression<string> ConstructLikeExpression (IExpression expression)
		{
			expression = new UnaryExpression<string, string>((a) => Regex.Escape(a), expression);
			expression = new ReplaceFunction (expression, new ConstExpression<string> ("%"), new ConstExpression<string> (".*"), false);
			expression = new ReplaceFunction (expression, new ConstExpression<string> ("_"), new ConstExpression<string> ("."), false);
			expression = new BinaryExpression<string, string, string>((a, b) => a + b, PREFIX, expression); 
			return new BinaryExpression<string, string, string>((a, b) => a + b, expression, SUFFIX); 
		}
	}
}

