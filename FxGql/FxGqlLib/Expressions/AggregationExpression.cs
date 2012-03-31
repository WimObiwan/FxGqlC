using System;

namespace FxGqlLib
{
	public class AggregationExpression<T, S, R> : Expression<R>
		where T : IComparable
		where R : IComparable
	{
		Func<T, S> init;
		Func<S, T, S> aggregator;
		Func<S, R> calculate;
		Expression<T> arg;
		
		public AggregationExpression (Func<T, S> init, Func<S, T, S> aggregator, Func<S, R> calculate, Expression<T> arg)
		{
			this.init = init;
			this.aggregator = aggregator;
			this.calculate = calculate;
			this.arg = arg;
		}

		#region implemented abstract members of FxGqlLib.Expression[T]
		public override R Evaluate (GqlQueryState gqlQueryState)
		{
			throw new NotSupportedException();
		}
		
		public override bool IsAggregated ()
		{
			return true;
		}
		
		public override void Aggregate (AggregationState state, GqlQueryState gqlQueryState)
		{
			S stateValue;
			T t = arg.Evaluate(gqlQueryState);
			if (!state.GetState<S>(this, out stateValue))
				stateValue = init(t);
			else
				stateValue = aggregator(stateValue, t);
			state.SetState(this, stateValue);
		}
		
		public override IComparable AggregateCalculate (AggregationState state)
		{
			S stateValue;
			if (!state.GetState<S>(this, out stateValue))
				throw new NotSupportedException("Aggregation state not found"); // TODO: Aggregation without values
			return calculate(stateValue);
		}
		#endregion
	}
}

