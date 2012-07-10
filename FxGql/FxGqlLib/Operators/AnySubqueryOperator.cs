using System;
using System.Collections.Generic;

namespace FxGqlLib
{
	public class AnySubqueryOperator<T> : Expression<bool> where T: IComparable
	{
		readonly Expression<T> arg;
		readonly IProvider provider;
		readonly Func<T, T, bool> functor;

		List<T> values;
		
		public AnySubqueryOperator (Expression<T> arg, 
		                       IProvider provider, 
		                       Func<T, T, bool> functor)
		{
			this.arg = arg;
			this.provider = provider;
			this.functor = functor;
		}
		
		#region implemented abstract members of FxGqlLib.Expression[System.Boolean]
		public override bool Evaluate (GqlQueryState gqlQueryState)
		{
			T value1 = arg.Evaluate (gqlQueryState);


			if (values == null) {
				values = new List<T> ();
				provider.Initialize (gqlQueryState);
				
				while (provider.GetNextRecord()) {
					values.Add ((T)Convert.ChangeType (provider.Record.Columns [0], typeof(T)));
				}					
				
				provider.Uninitialize ();
			}

			foreach (T value2 in values) {
				if (functor (value1, value2))
					return true;
			}
			return false;
		}
		#endregion
	}
}

