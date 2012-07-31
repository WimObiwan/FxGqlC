using System;

namespace FxGqlLib
{
	public static class ConvertExpression
	{
		public static IExpression Create (Type type, IExpression expr)
		{
			IExpression result;
			if (type == typeof(DataInteger)) {
				result = CreateDataInteger (expr);
			} else if (type == typeof(DataString)) {
				result = CreateDataString (expr);
			} else if (type == typeof(DataBoolean)) {
				result = CreateDataBoolean (expr);
			} else if (type == typeof(DataDateTime)) {
				result = CreateDataDateTime (expr);
			} else if (type == typeof(IData)) {
				result = CreateData (expr);
			} else {
				throw new Exception (string.Format ("Invalid conversion.  Datatype {0} unknown.", type.ToString ()));
			}

			return result;
		}

		public static IExpression Create (Type type, IExpression expr, string format)
		{
			if (format == null)
				return Create (type, expr);

			IExpression result;
			if (type == typeof(DataInteger)) {
				result = CreateDataInteger (expr, format);
			} else if (type == typeof(DataString)) {
				result = CreateDataString (expr, format);
			} else if (type == typeof(DataBoolean)) {
				result = CreateDataBoolean (expr);
			} else if (type == typeof(DataDateTime)) {
				result = CreateDataDateTime (expr, format);
			} else if (type == typeof(IData)) {
				result = CreateData (expr);
			} else {
				throw new Exception (string.Format ("Invalid conversion.  Datatype {0} unknown.", type.ToString ()));
			}

			return result;
		}

		public static Expression<DataInteger> CreateDataInteger (IExpression expr)
		{
			Expression<DataInteger> result = expr as Expression<DataInteger>;
			if (result == null)
				result = new ConvertExpression<DataInteger> ((a) => a.ToDataInteger (), expr);

			return result;
		}

		public static Expression<DataInteger> CreateDataInteger (IExpression expr, string format)
		{
			Expression<DataInteger> result = expr as Expression<DataInteger>;
			if (result == null)
				result = new ConvertExpression<DataInteger> ((a) => a.ToDataInteger (format), expr);

			return result;
		}

		public static Expression<DataString> CreateDataString (IExpression expr)
		{
			Expression<DataString> result = expr as Expression<DataString>;
			if (result == null)
				result = new ConvertExpression<DataString> ((a) => a.ToDataString (), expr);

			return result;
		}

		public static Expression<DataString> CreateDataString (IExpression expr, string format)
		{
			Expression<DataString> result = expr as Expression<DataString>;
			if (result == null)
				result = new ConvertExpression<DataString> ((a) => a.ToDataString (format), expr);

			return result;
		}

		public static Expression<DataBoolean> CreateDataBoolean (IExpression expr)
		{
			Expression<DataBoolean> result = expr as Expression<DataBoolean>;
			if (result == null)
				result = new ConvertExpression<DataBoolean> ((a) => a.ToDataBoolean (), expr);

			return result;
		}

		static IExpression CreateDataDateTime (IExpression expr)
		{
			Expression<DataDateTime> result = expr as Expression<DataDateTime>;
			if (result == null)
				result = new ConvertExpression<DataDateTime> ((a) => a.ToDataDateTime (), expr);

			return result;
		}

		static IExpression CreateDataDateTime (IExpression expr, string format)
		{
			Expression<DataDateTime> result = expr as Expression<DataDateTime>;
			if (result == null)
				result = new ConvertExpression<DataDateTime> ((a) => a.ToDataDateTime (format), expr);

			return result;
		}

		public static Expression<IData> CreateData (IExpression expr)
		{
			Expression<IData> result = expr as Expression<IData>;
			if (result == null)
				result = new ConvertExpression<IData> ((a) => a, expr);

			return result;
		}
	}

	public class ConvertExpression<T> : Expression<T> where T : IData
	{
		readonly IExpression expr;
		readonly Func<IData, T> functor;

		public ConvertExpression (Func<IData, T> functor, IExpression expr)
		{
			this.expr = expr;
			this.functor = functor;
		}

		#region implemented abstract members of FxGqlLib.Expression
		public override T Evaluate (GqlQueryState gqlQueryState)
		{
			return functor (expr.EvaluateAsData (gqlQueryState));
		}
		#endregion

		public override bool IsAggregated ()
		{
			return expr.IsAggregated ();
		}

		public override bool IsConstant ()
		{
			return expr.IsConstant ();
		}

		public override void Aggregate (StateBin state, GqlQueryState gqlQueryState)
		{
			expr.Aggregate (state, gqlQueryState);
		}
		
		public override IData AggregateCalculate (StateBin state)
		{
			return expr.AggregateCalculate (state);
		}
	}
}

