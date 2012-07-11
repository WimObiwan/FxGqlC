using System;

namespace FxGqlLib
{
	public class BinaryExpression<T1, T2, R> : Expression<R>
		where T1 : IComparable
		where T2 : IComparable
		where R : IComparable
	{
		readonly Func<T1, T2, R> functor;
		readonly Expression<T1> arg1;
		readonly Expression<T2> arg2;

		public BinaryExpression (Func<T1, T2, R> functor, IExpression arg1, IExpression arg2)
			: this (functor, ExpressionHelper.ConvertIfNeeded<T1>(arg1), ExpressionHelper.ConvertIfNeeded<T2>(arg2))
		{
		}
		
		public BinaryExpression (Func<T1, T2, R> functor, Expression<T1> arg1, Expression<T2> arg2)
		{
			this.functor = functor;
			this.arg1 = arg1;
			this.arg2 = arg2;
		}
		
		#region implemented abstract members of FxGqlLib.Expression[R]
		public override R Evaluate (GqlQueryState gqlQueryState)
		{
			return functor (arg1.Evaluate (gqlQueryState), arg2.Evaluate (gqlQueryState));
		}

		public override bool IsAggregated ()
		{
			return arg1.IsAggregated ();
		}

		public override bool IsConstant ()
		{
			return arg1.IsConstant () && arg2.IsConstant ();
		}
		
		public override void Aggregate (StateBin state, GqlQueryState gqlQueryState)
		{
			arg1.Aggregate (state, gqlQueryState);
			arg2.Aggregate (state, gqlQueryState);
		}
		
		public override IComparable AggregateCalculate (StateBin state)
		{
			T1 t1 = (T1)arg1.AggregateCalculate (state);
			T2 t2 = (T2)arg2.AggregateCalculate (state);
			return functor (t1, t2);
		}
		#endregion
	}
}

