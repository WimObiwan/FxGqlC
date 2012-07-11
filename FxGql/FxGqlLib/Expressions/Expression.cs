using System;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace FxGqlLib
{
	public interface IExpression
	{
		IComparable EvaluateAsComparable (GqlQueryState gqlQueryState);

		Y EvaluateAs<Y> (GqlQueryState gqlQueryState);

		string EvaluateAsString (GqlQueryState gqlQueryState);

		Type GetResultType ();
		
		// TODO: IsInputDependent(); for optimalisation
		// TODO: IsTimeDependent(); for optimalisation
		
		bool IsAggregated ();

		bool IsConstant ();

		void Aggregate (StateBin state, GqlQueryState gqlQueryState);
		
		IComparable AggregateCalculate (StateBin state);
	}

	public abstract class Expression<T> : IExpression where T : IComparable
	{
		public Expression ()
		{
		}
		
		public abstract T Evaluate (GqlQueryState gqlQueryState);

		#region IExpression implementation
		public virtual IComparable EvaluateAsComparable (GqlQueryState gqlQueryState)
		{
			T val = Evaluate (gqlQueryState);
			return val;
		}
		
		public virtual Y EvaluateAs<Y> (GqlQueryState gqlQueryState)
		{
			T val = Evaluate (gqlQueryState);
			if (val is IData)
				return DataConversion.As<Y> ((IData)val);
			else
				return (Y)Convert.ChangeType (val, typeof(Y));
		}

		public virtual string EvaluateAsString (GqlQueryState gqlQueryState)
		{
			return Evaluate (gqlQueryState).ToString ();
		}
		
		public virtual Type GetResultType ()
		{
			return typeof(T);
		}

		public virtual bool IsAggregated ()
		{
			return false;
		}

		public virtual bool IsConstant ()
		{
			return false;
		}

		public virtual void Aggregate (StateBin state, GqlQueryState gqlQueryState)
		{
			throw new Exception (string.Format ("Aggregation not supported on expression {0}", this.GetType ().ToString ()));
		}
		
		public virtual IComparable AggregateCalculate (StateBin state)
		{
			throw new Exception (string.Format ("Aggregation not supported on expression {0}", this.GetType ().ToString ()));
		}
		#endregion
	}

	/*
	public class DataExpression : Expression<IData>
	{
	}
	*/
}

