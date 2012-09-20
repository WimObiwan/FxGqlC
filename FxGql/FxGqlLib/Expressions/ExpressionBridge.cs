using System;
using System.Linq.Expressions;

namespace FxGqlLib
{
	[Obsolete]
	public class ExpressionBridge : IExpression
	{
		public ExpressionBridge (Expression linqExpression)
		{
		}

		#region IExpression implementation

		public IData EvaluateAsData (GqlQueryState gqlQueryState)
		{
			throw new NotImplementedException ();
		}

		public Type GetResultType ()
		{
			throw new NotImplementedException ();
		}

		public bool IsAggregated ()
		{
			throw new NotImplementedException ();
		}

		public bool IsConstant ()
		{
			throw new NotImplementedException ();
		}

		public void Aggregate (StateBin state, GqlQueryState gqlQueryState)
		{
			throw new NotImplementedException ();
		}

		public IData AggregateCalculate (StateBin state)
		{
			throw new NotImplementedException ();
		}

		#endregion

		public static System.Linq.Expressions.Expression Create (IExpression expr, System.Linq.Expressions.ParameterExpression queryStatePrm)
		{
			Type type = expr.GetResultType ();
			if (type == typeof(DataBoolean)) {
				return CreateBoolean (ConvertExpression.CreateDataBoolean (expr), queryStatePrm);
			} else {
				throw new NotSupportedException ();
			}
			
		}

		static System.Reflection.MethodInfo EvaluateAsDataMethod = typeof(IExpression).GetMethod ("EvaluateAsData");
		static System.Reflection.MethodInfo ToDataBooleanMethod = typeof(IData).GetMethod ("ToDataBoolean");
		static System.Reflection.PropertyInfo DataBooleanValue = typeof(DataBoolean).GetProperty ("Value");

		private static System.Linq.Expressions.Expression CreateBoolean (Expression<DataBoolean> expr, System.Linq.Expressions.ParameterExpression queryStatePrm)
		{
			return
				System.Linq.Expressions.Expression.Property (
					System.Linq.Expressions.Expression.Call (
						System.Linq.Expressions.Expression.Call (
							System.Linq.Expressions.Expression.Constant (expr),
							EvaluateAsDataMethod, queryStatePrm),
						ToDataBooleanMethod),
					DataBooleanValue);
		}
	}
}

