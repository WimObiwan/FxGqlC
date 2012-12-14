using System;
using System.Collections.Generic;

namespace FxGqlLib
{
	public class AnySubqueryOperator<T> : Expression<DataBoolean> where T: IData
	{
		readonly Expression<T> arg;
		readonly IProvider provider;
		readonly Func<T, T, DataBoolean> functor;

		List<T> values;
		
		public AnySubqueryOperator (Expression<T> arg, 
		                       IProvider provider, 
		                       Func<T, T, DataBoolean> functor)
		{
			this.arg = arg;
			this.provider = provider;
			this.functor = functor;
		}
		
		#region implemented abstract members of FxGqlLib.Expression[System.Boolean]
		public override DataBoolean Evaluate (GqlQueryState gqlQueryState)
		{
			T value1 = arg.Evaluate (gqlQueryState);

			if (values == null) {
				values = new List<T> ();

				Type[] types = this.provider.GetColumnTypes ();
				if (types.Length != 1) 
					throw new InvalidOperationException ("Subquery should contain only 1 column");
				IProvider provider;
				if (types [0] != typeof(T))
					provider = new ColumnProvider (new List<IExpression> () { new ColumnExpression<T> (this.provider, 0)}, this.provider);
				else
					provider = this.provider;
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

