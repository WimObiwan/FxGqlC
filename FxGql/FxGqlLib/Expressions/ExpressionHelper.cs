using System;

namespace FxGqlLib
{
	public static class ExpressionHelper
	{
		public static Expression<T> ConvertIfNeeded<T> (IExpression expression) where T : IComparable
		{
			if (expression is Expression<T>) {
				return (Expression<T>)expression;
			} else {
				return new ConvertExpression<T> (expression);
			}
		}

		public static Expression<string> ConvertToStringIfNeeded (IExpression expression)
		{
			if (expression is Expression<string>) {
				return (Expression<string>)expression;
			} else {
				return new ConvertToStringExpression (expression);
			}
		}
		
		public static T GetExpression<T> (IExpression expression) where T : IExpression
		{
			if (expression is T)
				return (T)expression;
			throw new Exception ("Incorrect datatype");
		}
	}
}

