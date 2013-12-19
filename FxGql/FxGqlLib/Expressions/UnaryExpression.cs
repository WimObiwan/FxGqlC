using System;
using System.Globalization;

namespace FxGqlLib
{
	public class UnaryExpression<T, R> : Expression<R>
		where T : IData
		where R : IData
	{
		readonly Func<T, R> functor;
		readonly Expression<T> arg;

		public static UnaryExpression<T, R> CreateAutoConvert (Func<T, R> functor, IExpression arg, CultureInfo cultureInfo)
		{
			Expression<T> typedArg = (Expression<T>)ConvertExpression.Create (typeof(T), arg, cultureInfo);
			return new UnaryExpression<T, R> (functor, typedArg);
		}

		public UnaryExpression (Func<T, R> functor, Expression<T> arg)
		{
			this.functor = functor;
			this.arg = arg;
		}

		#region implemented abstract members of FxGqlLib.Expression[R]

		public override R Evaluate (GqlQueryState gqlQueryState)
		{
			return functor (arg.Evaluate (gqlQueryState));
		}

		public override bool IsAggregated ()
		{
			return arg.IsAggregated ();
		}

		public override bool IsConstant ()
		{
			return arg.IsConstant ();
		}

		public override void Aggregate (StateBin state, GqlQueryState gqlQueryState)
		{
			arg.Aggregate (state, gqlQueryState);
		}

		public override IData AggregateCalculate (StateBin state)
		{
			return functor ((T)arg.AggregateCalculate (state));
		}

		#endregion

	}
}

