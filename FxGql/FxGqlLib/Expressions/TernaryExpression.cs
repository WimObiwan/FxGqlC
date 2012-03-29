using System;

namespace FxGqlLib
{
	public class TernaryExpression<T1, T2, T3, R> : Expression<R>
		where T1 : IComparable
		where T2 : IComparable
		where T3 : IComparable
		where R : IComparable
	{
		Func<T1, T2, T3, R> functor;
		Expression<T1> arg1;
		Expression<T2> arg2;
		Expression<T3> arg3;

		public TernaryExpression (Func<T1, T2, T3, R> functor, IExpression arg1, IExpression arg2, IExpression arg3)
			: this (functor, ExpressionHelper.ConvertIfNeeded<T1>(arg1), ExpressionHelper.ConvertIfNeeded<T2>(arg2),
			        ExpressionHelper.ConvertIfNeeded<T3>(arg3))
		{
		}
		
		public TernaryExpression (Func<T1, T2, T3, R> functor, Expression<T1> arg1, Expression<T2> arg2, Expression<T3> arg3)
		{
			this.functor = functor;
			this.arg1 = arg1;
			this.arg2 = arg2;
			this.arg3 = arg3;
		}
		
		#region implemented abstract members of FxGqlLib.Expression[R]
		public override R Evaluate (GqlQueryState gqlQueryState)
		{
			return functor (arg1.Evaluate (gqlQueryState), arg2.Evaluate (gqlQueryState), arg3.Evaluate (gqlQueryState));
		}

		public override bool IsAggregated ()
		{
			return arg1.IsAggregated () || arg2.IsAggregated () || arg3.IsAggregated ();
		}
		
		public override void Aggregate (AggregationState state, GqlQueryState gqlQueryState)
		{
			if (arg1.IsAggregated()) arg1.Aggregate (state, gqlQueryState);
			if (arg2.IsAggregated()) arg2.Aggregate (state, gqlQueryState);
			if (arg3.IsAggregated()) arg3.Aggregate (state, gqlQueryState);
		}
		
		public override IComparable AggregateCalculate (AggregationState state)
		{
			T1 t1;
			if (arg1.IsAggregated()) t1 = (T1)arg1.AggregateCalculate (state);
			else t1 = arg1.Evaluate(null);
			T2 t2;
			if (arg2.IsAggregated()) t2 = (T2)arg2.AggregateCalculate (state);
			else t2 = arg2.Evaluate(null);
			T3 t3;
			if (arg3.IsAggregated()) t3 = (T3)arg3.AggregateCalculate (state);
			else t3 = arg3.Evaluate(null);
			return functor(t1, t2, t3);
		}
		#endregion
	}
}

