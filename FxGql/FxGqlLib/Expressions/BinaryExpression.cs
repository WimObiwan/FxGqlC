using System;

namespace FxGqlLib
{
	public class BinaryExpression<T1, T2, R> : Expression<R>
		where T1 : IComparable
		where T2 : IComparable
		where R : IComparable
	{
		Func<T1, T2, R> functor;
		Expression<T1> arg1;
		Expression<T2> arg2;

		public BinaryExpression (System.Linq.Expressions.Expression<Func<T1, T2, R>> functor, IExpression arg1, IExpression arg2)
			: this (functor, ExpressionHelper.ConvertIfNeeded<T1>(arg1), ExpressionHelper.ConvertIfNeeded<T2>(arg2)) 
		{
		}
		
		public BinaryExpression (System.Linq.Expressions.Expression<Func<T1, T2, R>> functor, Expression<T1> arg1, Expression<T2> arg2)
		{
			this.functor = functor.Compile();
			this.arg1 = arg1;
			this.arg2 = arg2;
		}
		
		#region implemented abstract members of FxGqlLib.Expression[R]
		public override R Evaluate (GqlQueryState gqlQueryState)
		{
			return functor(arg1.Evaluate(gqlQueryState), arg2.Evaluate(gqlQueryState));
		}
		#endregion
	}
}

