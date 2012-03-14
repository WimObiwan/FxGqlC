using System;

namespace FxGqlLib
{
	public class UnaryExpression<T, R> : Expression<R>
		where T : IComparable
		where R : IComparable
	{
		Func<T, R> functor;
		Expression<T> arg;
		
		public UnaryExpression (System.Linq.Expressions.Expression<Func<T, R>> functor, IExpression arg)
			: this (functor, ExpressionHelper.ConvertIfNeeded<T>(arg)) 
		{
		}
		
		public UnaryExpression (System.Linq.Expressions.Expression<Func<T, R>> functor, Expression<T> arg)
		{
			this.functor = functor.Compile();
			this.arg = arg;
		}
		
		#region implemented abstract members of FxGqlLib.Expression[R]
		public override R Evaluate (GqlQueryState gqlQueryState)
		{
			return functor(arg.Evaluate(gqlQueryState));
		}
		#endregion
	}
}

