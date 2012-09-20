using System;

namespace FxGqlLib
{
	public static class NewConvertExpression
	{
		public static System.Linq.Expressions.Expression Create (Type type, System.Linq.Expressions.Expression expr, string format)
		{
			if (type == typeof(bool))
				return ToBoolean (expr, format);
			else if (type == typeof(bool))
				return ToString (expr, format);
			else if (type == typeof(bool))
				return ToLong (expr, format);
			else if (type == typeof(bool))
				return ToDateTime (expr, format);
			else 
				throw new NotSupportedException ();
		}

		static System.Linq.Expressions.Expression ToBoolean (System.Linq.Expressions.Expression expr, string format)
		{
			Type type = expr.Type;
			if (type == typeof(bool))
				return expr;
			else 
				throw new ConversionException (type, typeof(bool));
		}

		static System.Linq.Expressions.Expression ToString (System.Linq.Expressions.Expression expr, string format)
		{
			Type type = expr.Type;
			throw new ConversionException (type, typeof(string));
		}

		static System.Linq.Expressions.Expression ToLong (System.Linq.Expressions.Expression expr, string format)
		{
			Type type = expr.Type;
			throw new ConversionException (type, typeof(long));
		}
		static System.Linq.Expressions.Expression ToDateTime (System.Linq.Expressions.Expression expr, string format)
		{
			Type type = expr.Type;
			throw new ConversionException (type, typeof(DateTime));
		}
	}
}

