using System;

namespace FxGqlLib
{
	public static class ConvertExpression
	{
		public static IExpression Create (Type type, IExpression expression)
		{
			if (type == typeof(DataString)) {
				return new ConvertExpression<DataString> (expression);
			} else if (type == typeof(DataInteger)) {
				return new ConvertExpression<DataInteger> (expression);
			} else {
				throw new Exception (string.Format ("Unknown datatype {0}", type.ToString ()));
			}
		}
	}

	public class ConvertExpression<ToT> : Expression<ToT> where ToT : IComparable
	{
		readonly protected IExpression expression;
			
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
		
		public override void Aggregate (StateBin state, GqlQueryState gqlQueryState)
		{
			expression.Aggregate (state, gqlQueryState);
		}
		
		public override IComparable AggregateCalculate (StateBin state)
		{
			return (IComparable)Convert.ChangeType (expression.AggregateCalculate (state), typeof(ToT));
		}
		#endregion
	}
}

