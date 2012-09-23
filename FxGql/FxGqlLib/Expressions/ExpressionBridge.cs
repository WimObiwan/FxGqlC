using System;
using System.Linq.Expressions;
using System.Reflection;

namespace FxGqlLib
{
	[Obsolete]
	public static class ExpressionBridge
	{
		public static T ConvertFromOld<T> (IData data)
		{
			if (typeof(T) == typeof(bool))
				return (T)(object)data.ToDataBoolean ().Value;
			else if (typeof(T) == typeof(string))
				return (T)(object)data.ToDataString ().Value;
			else if (typeof(T) == typeof(long))
				return (T)(object)data.ToDataInteger ().Value;
			else if (typeof(T) == typeof(DateTime))
				return (T)(object)data.ToDataDateTime ().Value;
			else 
				throw new NotSupportedException ();
		}

		public static Type GetNewType (Type type)
		{
			if (type == typeof(DataBoolean)) {
				return typeof(bool);
			} else if (type == typeof(DataString)) {
				return typeof(string);
			} else if (type == typeof(DataInteger)) {
				return typeof(long);
			} else if (type == typeof(DataDateTime)) {
				return typeof(DateTime);
			} else {
				throw new NotSupportedException ();
			}
		}

		public static System.Linq.Expressions.Expression Create (IExpression expr, System.Linq.Expressions.ParameterExpression queryStatePrm)
		{
			Type type = expr.GetResultType ();
			if (type == typeof(DataBoolean)) {
				return CreateBoolean (ConvertExpression.CreateDataBoolean (expr), queryStatePrm);
			} else if (type == typeof(DataString)) {
				return CreateString (ConvertExpression.CreateDataString (expr), queryStatePrm);
			} else if (type == typeof(DataInteger)) {
				return CreateInteger (ConvertExpression.CreateDataInteger (expr), queryStatePrm);
			} else if (type == typeof(DataDateTime)) {
				return CreateDateTime (ConvertExpression.CreateDataDateTime (expr), queryStatePrm);
			} else {
				throw new NotSupportedException ();
			}
			
		}

		static MethodInfo EvaluateAsDataMethod = typeof(IExpression).GetMethod ("EvaluateAsData");
		static MethodInfo ToDataBooleanMethod = typeof(IData).GetMethod ("ToDataBoolean", new Type[] {});
		static PropertyInfo DataBooleanValue = typeof(DataBoolean).GetProperty ("Value");
		static MethodInfo ToDataStringMethod = typeof(IData).GetMethod ("ToDataString", new Type[] {});
		static PropertyInfo DataStringValue = typeof(DataString).GetProperty ("Value");
		static MethodInfo ToDataIntegerMethod = typeof(IData).GetMethod ("ToDataInteger", new Type[] {});
		static PropertyInfo DataIntegerValue = typeof(DataInteger).GetProperty ("Value");
		static MethodInfo ToDataDateTimeMethod = typeof(IData).GetMethod ("ToDataDateTime", new Type[] {});
		static PropertyInfo DataDateTimeValue = typeof(DataDateTime).GetProperty ("Value");

		private static System.Linq.Expressions.Expression CreateBoolean (Expression<DataBoolean> expr, 
		                                                                 System.Linq.Expressions.ParameterExpression queryStatePrm)
		{
			return CreateInternal (expr, queryStatePrm, ToDataBooleanMethod, DataBooleanValue);
		}
		
		private static System.Linq.Expressions.Expression CreateString (Expression<DataString> expr, 
		                                                                 System.Linq.Expressions.ParameterExpression queryStatePrm)
		{
			return CreateInternal (expr, queryStatePrm, ToDataStringMethod, DataStringValue);
		}
		
		private static System.Linq.Expressions.Expression CreateInteger (Expression<DataInteger> expr, 
		                                                                 System.Linq.Expressions.ParameterExpression queryStatePrm)
		{
			return CreateInternal (expr, queryStatePrm, ToDataIntegerMethod, DataIntegerValue);
		}
		
		private static System.Linq.Expressions.Expression CreateDateTime (Expression<DataDateTime> expr, 
		                                                                 System.Linq.Expressions.ParameterExpression queryStatePrm)
		{
			return CreateInternal (expr, queryStatePrm, ToDataDateTimeMethod, DataDateTimeValue);
		}
		
		private static System.Linq.Expressions.Expression CreateInternal<T> (Expression<T> expr, 
		                                                                     System.Linq.Expressions.ParameterExpression queryStatePrm,
		                                                                     MethodInfo conversionMethod,
		                                                                     PropertyInfo valueProperty) where T : IData
		{
			return
				System.Linq.Expressions.Expression.Property (
					System.Linq.Expressions.Expression.Call (
					System.Linq.Expressions.Expression.Call (
					System.Linq.Expressions.Expression.Constant (expr),
					EvaluateAsDataMethod, queryStatePrm),
					conversionMethod),
					valueProperty);
		}

	}
}

