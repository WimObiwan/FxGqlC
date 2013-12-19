using System;
using System.Globalization;

namespace FxGqlLib
{
	public static class ConvertExpression
	{
		public static IExpression Create (DataType type, IExpression expr, CultureInfo cultureInfo)
		{
			IExpression result;
			if (type == DataType.Integer) {
				result = CreateDataInteger (expr, cultureInfo);
			} else if (type == DataType.Float) {
				result = CreateDataFloat (expr, cultureInfo);
			} else if (type == DataType.String) {
				result = CreateDataString (expr, cultureInfo);
			} else if (type == DataType.Boolean) {
				result = CreateDataBoolean (expr, cultureInfo);
			} else if (type == DataType.DateTime) {
				result = CreateDataDateTime (expr, cultureInfo);
			} else {
				throw new Exception (string.Format ("Invalid conversion.  Datatype {0} unknown.", type.ToString ()));
			}
			
			return result;
		}

		public static IExpression Create (Type type, IExpression expr, CultureInfo cultureInfo)
		{
			IExpression result;
			if (type == typeof(DataInteger)) {
				result = CreateDataInteger (expr, cultureInfo);
			} else if (type == typeof(DataFloat)) {
				result = CreateDataFloat (expr, cultureInfo);
			} else if (type == typeof(DataString)) {
				result = CreateDataString (expr, cultureInfo);
			} else if (type == typeof(DataBoolean)) {
				result = CreateDataBoolean (expr, cultureInfo);
			} else if (type == typeof(DataDateTime)) {
				result = CreateDataDateTime (expr, cultureInfo);
			} else if (type == typeof(IData)) {
				result = CreateData (expr);
			} else {
				throw new Exception (string.Format ("Invalid conversion.  Datatype {0} unknown.", type.ToString ()));
			}

			return result;
		}

		public static IExpression Create (Type type, IExpression expr, CultureInfo cultureInfo, string format)
		{
			if (format == null)
				return Create (type, expr, cultureInfo);

			IExpression result;
			if (type == typeof(DataInteger)) {
				result = CreateDataInteger (expr, cultureInfo, format);
			} else if (type == typeof(DataFloat)) {
				result = CreateDataFloat (expr, cultureInfo, format);
			} else if (type == typeof(DataString)) {
				result = CreateDataString (expr, cultureInfo, format);
			} else if (type == typeof(DataBoolean)) {
				result = CreateDataBoolean (expr, cultureInfo);
			} else if (type == typeof(DataDateTime)) {
				result = CreateDataDateTime (expr, cultureInfo, format);
			} else if (type == typeof(IData)) {
				result = CreateData (expr);
			} else {
				throw new Exception (string.Format ("Invalid conversion.  Datatype {0} unknown.", type.ToString ()));
			}

			return result;
		}

		public static Expression<DataInteger> CreateDataInteger (IExpression expr, CultureInfo ci)
		{
			Expression<DataInteger> result = expr as Expression<DataInteger>;
			if (result == null)
				result = new ConvertExpression<DataInteger> ((a) => a.ToDataInteger (ci), expr);
			
			return result;
		}

		public static Expression<DataInteger> CreateDataInteger (IExpression expr, CultureInfo ci, string format)
		{
			Expression<DataInteger> result = expr as Expression<DataInteger>;
			if (result == null)
				result = new ConvertExpression<DataInteger> ((a) => a.ToDataInteger (ci, format), expr);
			
			return result;
		}

		public static Expression<DataFloat> CreateDataFloat (IExpression expr, CultureInfo ci)
		{
			Expression<DataFloat> result = expr as Expression<DataFloat>;
			if (result == null)
				result = new ConvertExpression<DataFloat> ((a) => a.ToDataFloat (ci), expr);
			
			return result;
		}

		public static Expression<DataFloat> CreateDataFloat (IExpression expr, CultureInfo ci, string format)
		{
			Expression<DataFloat> result = expr as Expression<DataFloat>;
			if (result == null)
				result = new ConvertExpression<DataFloat> ((a) => a.ToDataFloat (ci, format), expr);
			
			return result;
		}

		public static Expression<DataString> CreateDataString (IExpression expr, CultureInfo ci)
		{
			Expression<DataString> result = expr as Expression<DataString>;
			if (result == null)
				result = new ConvertExpression<DataString> ((a) => a.ToDataString (ci), expr);

			return result;
		}

		public static Expression<DataString> CreateDataString (IExpression expr, CultureInfo ci, string format)
		{
			Expression<DataString> result = expr as Expression<DataString>;
			if (result == null)
				result = new ConvertExpression<DataString> ((a) => a.ToDataString (ci, format), expr);

			return result;
		}

		public static Expression<DataBoolean> CreateDataBoolean (IExpression expr, CultureInfo ci)
		{
			Expression<DataBoolean> result = expr as Expression<DataBoolean>;
			if (result == null)
				result = new ConvertExpression<DataBoolean> ((a) => a.ToDataBoolean (ci), expr);

			return result;
		}

		static IExpression CreateDataDateTime (IExpression expr, CultureInfo ci)
		{
			Expression<DataDateTime> result = expr as Expression<DataDateTime>;
			if (result == null)
				result = new ConvertExpression<DataDateTime> ((a) => a.ToDataDateTime (ci), expr);

			return result;
		}

		static IExpression CreateDataDateTime (IExpression expr, CultureInfo ci, string format)
		{
			Expression<DataDateTime> result = expr as Expression<DataDateTime>;
			if (result == null)
				result = new ConvertExpression<DataDateTime> ((a) => a.ToDataDateTime (ci, format), expr);

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
			IData data = expr.EvaluateAsData (gqlQueryState);
			try {
				return functor (data);
			} catch (Exception x) {
				throw new RunTimeConversionException (expr.GetResultType (), typeof(T), data, x);
			}
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

