using System;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace FxGqlLib
{
	public interface IExpression
	{
		IData EvaluateAsData (GqlQueryState gqlQueryState);

		Type GetResultType ();
		// TODO: IsInputDependent(); for optimalisation
		// TODO: IsTimeDependent(); for optimalisation
		bool IsAggregated ();

        bool IsConstant ();

		void Process (StateBin state, GqlQueryState gqlQueryState); // For Aggregation e.g. LAST() or State e.g. PREVIOUS()

        IData ProcessCalculate (StateBin state);
	}

	public abstract class Expression<T> : IExpression where T : IData
	{
		public Expression ()
		{
		}

		public abstract T Evaluate (GqlQueryState gqlQueryState);

		#region IExpression implementation

		public virtual IData EvaluateAsData (GqlQueryState gqlQueryState)
		{
			T val = Evaluate (gqlQueryState);
			return val;
		}

		public virtual Type GetResultType ()
		{
			return typeof(T);
		}

        public virtual bool IsAggregated()
        {
            return false;
        }

        public virtual bool IsConstant ()
		{
			return false;
		}

		public virtual void Process (StateBin state, GqlQueryState gqlQueryState)
		{
			throw new Exception (string.Format ("Aggregation not supported on expression {0}", this.GetType ().ToString ()));
		}

		public virtual IData ProcessCalculate (StateBin state)
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

