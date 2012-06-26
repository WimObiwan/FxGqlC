using System;

namespace FxGqlLib
{
	public static class ConvertExpression
	{
		public static IExpression Create (Type type, IExpression expression)
		{
			if (type == typeof(string)) {
				return new ConvertExpression<string> (expression);
			} else if (type == typeof(long)) {
				return new ConvertExpression<long> (expression);
			} else {
				throw new Exception (string.Format ("Unknown datatype {0}", type.ToString ()));
			}
		}
	}

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
			return (IComparable)Convert.ChangeType (expression.AggregateCalculate (state), typeof(ToT));
		}
		#endregion
	}
}

