using System;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace FxGqlLib
{
	public class AggregationState
	{
		static ObjectIDGenerator objectIDGenerator = new ObjectIDGenerator ();
		
		private static long GetId (object obj)
		{
			bool notused;
			return objectIDGenerator.GetId (obj, out notused);
		}
		
		Dictionary<long, object> state = new Dictionary<long, object> ();
		
		public bool GetState<T> (object obj, out T val)
		{
			object objVal;
			bool result = state.TryGetValue (GetId (obj), out objVal);
			if (result)
				val = (T)objVal;
			else
				val = default(T);
			return result;
		}
		
		public void SetState<T> (object obj, T val)
		{
			state [GetId (obj)] = val;
		}
	}

	public interface IExpression
	{
		IComparable EvaluateAsComparable (GqlQueryState gqlQueryState);

		Y EvaluateAs<Y> (GqlQueryState gqlQueryState);

		string EvaluateAsString (GqlQueryState gqlQueryState);

		Type GetResultType ();
		
		// TODO: IsInputDependent(); for optimalisation
		// TODO: IsTimeDependent(); for optimalisation
		
		bool IsAggregated ();
		
		void Aggregate (AggregationState state, GqlQueryState gqlQueryState);
		
		IComparable AggregateCalculate (AggregationState state);
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
		
		public virtual void Aggregate (AggregationState state, GqlQueryState gqlQueryState)
		{
			throw new Exception (string.Format ("Aggregation not supported on expression {0}", this.GetType ().ToString ()));
		}
		
		public virtual IComparable AggregateCalculate (AggregationState state)
		{
			throw new Exception (string.Format ("Aggregation not supported on expression {0}", this.GetType ().ToString ()));
		}
		#endregion
	}
}

