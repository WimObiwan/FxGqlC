using System;

namespace FxGqlLib
{
	public class UnaryExpression<T, R> : Expression<R>
		where T : IComparable
		where R : IComparable
	{
		readonly Func<T, R> functor;
		readonly Expression<T> arg;
		
		public UnaryExpression (Func<T, R> functor, IExpression arg)
			: this (functor, ExpressionHelper.ConvertIfNeeded<T>(arg))
		{
		}
		
		public UnaryExpression (Func<T, R> functor, Expression<T> arg)
		{
			this.functor = functor;
			this.arg = arg;
		}
		
		#region implemented abstract members of FxGqlLib.Expression[R]
		public override R Evaluate (GqlQueryState gqlQueryState)
		{
			return functor (arg.Evaluate (gqlQueryState));
		}
		
		public override bool IsAggregated ()
		{
			return arg.IsAggregated ();
		}
		
		public override bool IsConstant ()
		{
			return arg.IsConstant ();
		}

		public override void Aggregate (StateBin state, GqlQueryState gqlQueryState)
		{
			arg.Aggregate (state, gqlQueryState);
		}
		
		public override IComparable AggregateCalculate (StateBin state)
		{
			return functor ((T)arg.AggregateCalculate (state));
		}
		#endregion
	}
}

