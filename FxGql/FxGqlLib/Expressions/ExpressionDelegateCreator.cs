using System;

namespace FxGqlLib
{
	public static class ExpressionDelegateCreator
	{
		public static System.Linq.Expressions.Expression<Func<GqlQueryState, T>> Create<T> (
			System.Linq.Expressions.Expression expr,
			System.Linq.Expressions.ParameterExpression queryStatePrm)
		{
			if (expr.Type != typeof(T))
				throw new InvalidOperationException ();
			
			return
				System.Linq.Expressions.Expression.Lambda<Func<GqlQueryState, T>> (
					expr,
					new System.Linq.Expressions.ParameterExpression[] { queryStatePrm });
		}
	}
}

