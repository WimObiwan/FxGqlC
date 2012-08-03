using System;

namespace FxGqlLib
{
	public interface IColumnExpression : IExpression
	{
		ColumnName ColumnName { get; }
	}

	public class ColumnExpression<T> : Expression<T>, IColumnExpression where T : IData
	{
		readonly IProvider[] providers;
		readonly ColumnName columnName;
		readonly int origColumnOrdinal;

		int columnOrdinal;
		int upstreamLevel;

		private ColumnExpression (IProvider[] providers, ColumnName columnName, int columnOrdinal)
		{
			this.providers = providers;
			if (columnName != null)
				this.columnName = columnName;
			else if (providers.Length > 0) {
				ColumnName[] columns = providers [0].GetColumnNames ();
				if (columnOrdinal >= 0 && columnOrdinal < columns.Length)
					this.columnName = columns [columnOrdinal];
			}
			this.origColumnOrdinal = columnOrdinal;

			this.columnOrdinal = -1;
			this.upstreamLevel = -1;
		}

		public ColumnExpression (IProvider[] providers, ColumnName columnName)
			: this (providers, columnName, -1)
		{
		}
		
		public ColumnExpression (IProvider provider, ColumnName columnName)
			: this (new IProvider[] { provider }, columnName, -1)
		{
		}
		
		public ColumnExpression (IProvider provider, int columnOrdinal)
			: this (new IProvider[] { provider }, null, columnOrdinal)
		{
		}

		public ColumnName ColumnName {
			get {
				if (columnName != null)
					return columnName;
				if (upstreamLevel >= 0 && upstreamLevel < providers.Length) {
					ColumnName[] columnNames = providers [upstreamLevel].GetColumnNames ();
					if (origColumnOrdinal >= 0 && origColumnOrdinal < columnNames.Length)
						return columnNames [origColumnOrdinal];
					else 					if (columnOrdinal >= 0 && columnOrdinal < columnNames.Length)
						return columnNames [columnOrdinal];
				}
				return null;
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

			if (upstreamLevel == -1) {
				upstreamLevel = 0;
				if (origColumnOrdinal != -1) {
					columnOrdinal = origColumnOrdinal;
				} else {
					for (upstreamLevel = 0; upstreamLevel < providers.Length; upstreamLevel++) {
						columnOrdinal = providers [upstreamLevel].GetColumnOrdinal (columnName);
						if (columnOrdinal != -1)
							break;
					}
				}
			}

			if (upstreamLevel >= 0 && upstreamLevel < providers.Length) {
				IData[] columns;
				//ProviderRecord record = gqlQueryState.Record;
				ProviderRecord record = providers [upstreamLevel].Record;
				if ((gqlQueryState.UseOriginalColumns) && !(origColumnOrdinal != -1))
					columns = record.OriginalColumns;
				else
					columns = record.Columns;
				if (columnOrdinal >= 0 && columnOrdinal < columns.Length)
					try {
						return (T)columns [columnOrdinal];
					} catch (InvalidCastException) {
						throw new ConversionException (columns [columnOrdinal].GetType (), typeof(T));
					}
			}
			throw new Exception (string.Format ("Column {0} not found", columnName));
		}
		#endregion
	}
}

