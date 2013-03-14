using System;
using System.Threading.Tasks;
using System.Globalization;

namespace FxGqlLib
{
	public class BinaryExpression<T1, T2, R> : Expression<R>
		where T1 : IData
		where T2 : IData
		where R : IData
	{
		readonly Func<T1, T2, R> functor;
		readonly Expression<T1> arg1;
		readonly Expression<T2> arg2;

		public static BinaryExpression<T1, T2, R> CreateAutoConvert (Func<T1, T2, R> functor, IExpression arg1, IExpression arg2, CultureInfo cultureInfo)
		{
			Expression<T1> typedArg1 = (Expression<T1>)ConvertExpression.Create (typeof(T1), arg1, cultureInfo);
			Expression<T2> typedArg2 = (Expression<T2>)ConvertExpression.Create (typeof(T2), arg2, cultureInfo);
			return new BinaryExpression<T1, T2, R> (functor, typedArg1, typedArg2);
		}
		
		public BinaryExpression (Func<T1, T2, R> functor, Expression<T1> arg1, Expression<T2> arg2)
		{
			this.functor = functor;
			this.arg1 = arg1;
			this.arg2 = arg2;
		}
		
		#region implemented abstract members of FxGqlLib.Expression[R]
		public override R Evaluate (GqlQueryState gqlQueryState)
		{
//			Task<T1> task1 = Task.Factory.StartNew (() => arg1.Evaluate (gqlQueryState));
//			Task<T2> task2 = Task.Factory.StartNew (() => arg2.Evaluate (gqlQueryState));
//
//			Task.WaitAll (task1, task2);
//			return functor (task1.Result, task2.Result);

			return functor (arg1.Evaluate (gqlQueryState), arg2.Evaluate (gqlQueryState));
		}

		public override bool IsAggregated ()
		{
			return arg1.IsAggregated ();
		}

		public override bool IsConstant ()
		{
			return arg1.IsConstant () && arg2.IsConstant ();
		}
		
		public override void Aggregate (StateBin state, GqlQueryState gqlQueryState)
		{
			arg1.Aggregate (state, gqlQueryState);
			arg2.Aggregate (state, gqlQueryState);
		}
		
		public override IData AggregateCalculate (StateBin state)
		{
			T1 t1 = (T1)arg1.AggregateCalculate (state);
			T2 t2 = (T2)arg2.AggregateCalculate (state);
			return functor (t1, t2);
		}
		#endregion
	}
}

