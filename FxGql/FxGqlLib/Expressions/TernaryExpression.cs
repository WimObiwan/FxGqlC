using System;

namespace FxGqlLib
{
	public class TernaryExpression<T1, T2, T3, R> : Expression<R>
		where T1 : IComparable
		where T2 : IComparable
		where T3 : IComparable
		where R : IComparable
	{
		readonly Func<T1, T2, T3, R> functor;
		readonly Expression<T1> arg1;
		readonly Expression<T2> arg2;
		readonly Expression<T3> arg3;

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
			return arg1.IsAggregated ();
		}
		
		public override void Aggregate (StateBin state, GqlQueryState gqlQueryState)
		{
			arg1.Aggregate (state, gqlQueryState);
			arg2.Aggregate (state, gqlQueryState);
			arg3.Aggregate (state, gqlQueryState);
		}
		
		public override IComparable AggregateCalculate (StateBin state)
		{
			T1 t1 = (T1)arg1.AggregateCalculate (state);
			T2 t2 = (T2)arg2.AggregateCalculate (state);
			T3 t3 = (T3)arg3.AggregateCalculate (state);
			return functor (t1, t2, t3);
		}
		#endregion
	}
}

