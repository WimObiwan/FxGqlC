using System;

namespace FxGqlLib
{
	public class NullaryExpression<R> : Expression<R>
		where R : IData
	{
		readonly Func<R> functor;
		
		public NullaryExpression (Func<R> functor)
		{
			this.functor = functor;
		}
		
		#region implemented abstract members of FxGqlLib.Expression[R]
		public override R Evaluate (GqlQueryState gqlQueryState)
		{
			return functor ();
		}
		#endregion
	}
}

