using System;

namespace FxGqlLib
{
	public class ColumnExpression : IExpression
	{
		string column;
		IProvider provider;
		int columnOrdinal = -1;
		
		public ColumnExpression (IProvider provider, string column)
		{
			this.column = column;
			this.provider = provider;
		}
		
		#region IExpression implementation
		public IComparable EvaluateAsComparable (GqlQueryState gqlQueryState)
		{
			//for (int i = 0; i < gqlQueryState.Record.ColumnTitles.Length; i++) {
			//	if (string.Compare (gqlQueryState.Record.ColumnTitles [i], column, StringComparison.InvariantCultureIgnoreCase) == 0) {
			//		return gqlQueryState.Record.Columns [i];
			//	}
			//}
			if (columnOrdinal == -1)
				columnOrdinal = this.provider.GetColumnOrdinal(column);
			
			IComparable[] columns = gqlQueryState.Record.Columns;
			if (columnOrdinal >= 0 && columnOrdinal < columns.Length)
				return columns[columnOrdinal];
			else
				throw new Exception (string.Format ("Column {0} not found", column));
		}

		public Y EvaluateAs<Y> (GqlQueryState gqlQueryState)
		{
			IComparable value = EvaluateAsComparable (gqlQueryState);
			if (value is Y)
				return (Y)value;
			else
				return (Y)Convert.ChangeType (value, typeof(Y));
		}

		public string EvaluateAsString (GqlQueryState gqlQueryState)
		{
			IComparable value = EvaluateAsComparable (gqlQueryState);
			if (value is string)
				return (string)value;
			else
				return value.ToString ();
		}

		public Type GetResultType ()
		{
			if (columnOrdinal == -1) 
				columnOrdinal = this.provider.GetColumnOrdinal(column);
			
			Type[] types = provider.GetColumnTypes();
			if (columnOrdinal >= 0 && columnOrdinal < types.Length)
				return types[columnOrdinal];
			else
				throw new Exception (string.Format ("Column {0} not found", column));
		}
		#endregion
	}
}

