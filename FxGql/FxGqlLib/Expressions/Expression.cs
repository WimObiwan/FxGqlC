using System;

namespace FxGqlLib
{
	public interface IExpression
	{
		IComparable EvaluateAsComparable (GqlQueryState gqlQueryState);

		Y EvaluateAs<Y> (GqlQueryState gqlQueryState);

		string EvaluateAsString (GqlQueryState gqlQueryState);

		Type GetResultType ();
	}
	
	public abstract class Expression<T> : IExpression where T : IComparable
	{
		public Expression ()
		{
		}
		
		//public Type ResultType { get { return typeof(T); } }
		
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
		#endregion
	}
}

