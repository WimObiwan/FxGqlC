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

		public static Expression<DataInteger> ConvertToDataIntegerIfNeeded (IExpression expression)
		{
			if (expression is Expression<DataInteger>) {
				return expression as Expression<DataInteger>;
			} else {
				return new ConvertExpression<DataInteger> (expression);
			}
		}

		public static Expression<DataString> ConvertToStringIfNeeded (IExpression expression)
		{
			if (expression is Expression<DataString>) {
				return (Expression<DataString>)expression;
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

