using System;

namespace FxGqlLib
{
	public interface IColumnExpression : IExpression
	{
		ColumnName ColumnName { get; }
	}

	public class ColumnExpression<T> : Expression<T>, IColumnExpression where T : IComparable
	{
		ColumnName columnName;
		IProvider provider;
		int columnOrdinal = -1;
		
		public ColumnExpression (IProvider provider, ColumnName columnName)
		{
			this.columnName = columnName;
			this.provider = provider;
		}
		
		public ColumnExpression (IProvider provider, int column)
		{
			this.columnOrdinal = column;
			this.provider = provider;
		}

		public ColumnName ColumnName {
			get {
				if (columnName != null)
					return columnName;
				else
					return provider.GetColumnNames () [columnOrdinal];
			}
		}		

		#region IExpression implementation
		public override T Evaluate (GqlQueryState gqlQueryState)
		{
			//for (int i = 0; i < gqlQueryState.Record.ColumnTitles.Length; i++) {
			//	if (string.Compare (gqlQueryState.Record.ColumnTitles [i], column, StringComparison.InvariantCultureIgnoreCase) == 0) {
			//		return gqlQueryState.Record.Columns [i];
			//	}
			//}
			if (columnOrdinal == -1)
				columnOrdinal = this.provider.GetColumnOrdinal (columnName);
			
			IComparable[] columns;
			if (gqlQueryState.UseOriginalColumns)
				columns = gqlQueryState.Record.OriginalColumns;
			else
				columns = gqlQueryState.Record.Columns;
			if (columnOrdinal >= 0 && columnOrdinal < columns.Length)
				return (T)columns [columnOrdinal];
			else
				throw new Exception (string.Format ("Column {0} not found", columnName));
		}
		#endregion
	}
}

