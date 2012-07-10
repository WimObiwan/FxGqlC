using System;

namespace FxGqlLib
{
	public class AnyListOperator<T> : Expression<bool> where T: IComparable
	{
		readonly Expression<T> arg;
		readonly Expression<T>[] list;
		readonly Func<T, T, bool> functor;
		
		public AnyListOperator (Expression<T> arg, 
		                       IExpression[] list, 
		                       Func<T, T, bool> functor)
		{
			this.arg = arg;
			this.list = new Expression<T>[list.Length];
			for (int i = 0; i < list.Length; i++) {
				this.list [i] = ExpressionHelper.ConvertIfNeeded <T> (list [i]);
			}
			this.functor = functor;
		}
		
		#region implemented abstract members of FxGqlLib.Expression[System.Boolean]
		public override bool Evaluate (GqlQueryState gqlQueryState)
		{
			T value1 = arg.Evaluate (gqlQueryState);
			foreach (Expression<T> arg2 in list) {
				T value2 = arg2.Evaluate (gqlQueryState);
				if (functor (value1, value2))
					return true;
			}
			return false;
		}
		#endregion
	}
}

