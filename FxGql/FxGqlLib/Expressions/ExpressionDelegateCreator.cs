using System;

namespace FxGqlLib
{
	public static class ExpressionDelegateCreator
	{
		public static System.Linq.Expressions.Expression<Func<GqlQueryState, bool>> CreateBoolean (
			System.Linq.Expressions.Expression expr,
			System.Linq.Expressions.ParameterExpression queryStatePrm)
		{
			if (expr.Type != typeof(Boolean))
				throw new InvalidOperationException ();

			return
				System.Linq.Expressions.Expression.Lambda<Func<GqlQueryState, bool>> (
					expr,
					new System.Linq.Expressions.ParameterExpression[] { queryStatePrm });
		}
	}
}

