using System;

namespace FxGqlLib
{
	public class NullaryExpression<R> : Expression<R>
		where R : IComparable
	{
		readonly Func<R> functor;
		
		public NullaryExpression (Func<R> functor)
		{
			this.functor = functor;
		}
		
		#region implemented abstract members of FxGqlLib.Expression[R]
		public override R Evaluate (GqlQueryState gqlQueryState)
		{
			return functor ();
		}
		
		public override bool IsAggregated ()
		{
			return false;
		}
		
		public override void Aggregate (StateBin state, GqlQueryState gqlQueryState)
		{
			throw new InvalidOperationException ();
		}
		
		public override IComparable AggregateCalculate (StateBin state)
		{
			throw new InvalidOperationException ();
		}
		#endregion
	}
}

