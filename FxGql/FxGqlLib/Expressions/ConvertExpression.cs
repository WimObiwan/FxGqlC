using System;

namespace FxGqlLib
{
	public class ConvertExpression<ToT> : Expression<ToT> where ToT : IComparable
	{
		protected IExpression expression;
			
		public ConvertExpression (IExpression  expression)
		{
			this.expression = expression;
		}
		
		#region implemented abstract members of FxGqlLib.Expression[ToT]
		public override ToT Evaluate (GqlQueryState gqlQueryState)
		{
			return expression.EvaluateAs<ToT> (gqlQueryState);
		}
		
		public override bool IsAggregated ()
		{
			return expression.IsAggregated ();
		}
		
		public override void Aggregate (AggregationState state, GqlQueryState gqlQueryState)
		{
			expression.Aggregate (state, gqlQueryState);
		}
		
		public override IComparable AggregateCalculate (AggregationState state)
		{
			return (IComparable)Convert.ChangeType(expression.AggregateCalculate (state), typeof(ToT));
		}
		#endregion
	}
}

