using System;

namespace FxGqlLib
{
	public interface IColumnExpression : IExpression
	{
		ColumnName ColumnName { get; }
	}

	public class ColumnExpression<T> : Expression<T>, IColumnExpression where T : IComparable
	{
		readonly ColumnName columnName;
		readonly IProvider provider;
		readonly int origColumnOrdinal;

		int columnOrdinal;

		private ColumnExpression (IProvider provider, ColumnName columnName, int columnOrdinal)
		{
			this.columnName = columnName;
			this.provider = provider;
			this.origColumnOrdinal = columnOrdinal;

			this.columnOrdinal = -1;
		}

		public ColumnExpression (IProvider provider, ColumnName columnName)
			: this (provider, columnName, -1)
		{
		}
		
		public ColumnExpression (IProvider provider, int columnOrdinal)
			: this (provider, null, columnOrdinal)
		{
		}

		public ColumnName ColumnName {
			get {
				if (columnName != null)
					return columnName;
				else {
					ColumnName[] columnNames = provider.GetColumnNames ();
					if (origColumnOrdinal >= 0 && origColumnOrdinal < columnNames.Length)
						return columnNames [origColumnOrdinal];
					else 					if (columnOrdinal >= 0 && columnOrdinal < columnNames.Length)
						return columnNames [columnOrdinal];
					else
						return null;
				}
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

			if (columnOrdinal == -1) {
				if (origColumnOrdinal != -1)
					columnOrdinal = origColumnOrdinal;
				else
					columnOrdinal = provider.GetColumnOrdinal (columnName);
			}

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

