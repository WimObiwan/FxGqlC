using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FxGqlLib
{
	/*
	public class Column
	{ 
		public IExpression Expression { get; set; }

		public string Name { get; set; }
	}

	public class AllColums : Column
	{
		public AllColums (IProvider provider)
		{
			Provider = provider;
		}

		public IProvider Provider { get; private set; }
	}
	*/
	
	public class SelectProvider : IProvider
	{
		readonly IList<Column> outputColumns;
		readonly IExpression[] staticOutputList;
		readonly ColumnName[] staticColumnNameList;

		IExpression[] outputList;
		ColumnName[] columnNameList;
		IProvider provider;
		GqlQueryState gqlQueryState;
		ProviderRecord record;
		
		public SelectProvider (IList<IExpression> outputList, IProvider provider)
		{
			this.staticOutputList = outputList.ToArray ();
			this.staticColumnNameList = new ColumnName[outputList.Count];
			for (int i = 0; i < outputList.Count; i++) {
				IColumnExpression columnExpression = outputList [i] as IColumnExpression;
				if (columnExpression != null) {
					this.staticColumnNameList [i] = columnExpression.ColumnName;
				} else {
					this.staticColumnNameList [i] = new ColumnName (i);
				}
			}
			this.outputList = this.staticOutputList;
			this.columnNameList = this.staticColumnNameList;
			this.provider = provider;
		}

		public SelectProvider (IList<Column> outputColumns, IProvider provider)
		{
			if (!outputColumns.Any (p => p is AllColums)) {
				if (outputColumns != null) {
					List<IExpression> outputList = new List<IExpression> ();
					List<ColumnName> columnNameList = new List<ColumnName> ();
					for (int i = 0; i < outputColumns.Count; i++) {
						Column column = outputColumns [i];
						if (column is AllColums) {
							AllColums allColums = (AllColums)column;
							var columnNameList2 = allColums.Provider.GetColumnNames ();
							for (int j = 0; j < columnNameList2.Length; j++) {
								if (allColums.ProviderAlias == null 
									|| StringComparer.InvariantCultureIgnoreCase.Compare (allColums.ProviderAlias, columnNameList2 [j].Alias) == 0) {
									outputList.Add (GqlParser.ConstructColumnExpression (allColums.Provider, j));
									columnNameList.Add (columnNameList2 [j]);
								}
							}
							if (columnNameList.Count == 0)
								throw new InvalidOperationException (string.Format ("No columns found for provider alias {0}", allColums.ProviderAlias));
						} else if (column is SingleColumn) {
							SingleColumn singleColumn = (SingleColumn)column;
							outputList.Add (singleColumn.Expression);
							columnNameList.Add (singleColumn);
						} else {
							throw new InvalidOperationException (string.Format ("Unknown column type {0}", column.GetType ()));
						}
					}
					this.staticOutputList = outputList.ToArray ();
					this.staticColumnNameList = columnNameList.ToArray ();
					for (int i = 0; i < columnNameList.Count; i++)
						if (this.staticColumnNameList [i] == null)
							this.staticColumnNameList [i] = new ColumnName (i);
				}
			} else {
				this.outputColumns = outputColumns;
			}
			this.provider = provider;
		}

		#region IProvider implementation
		public string[] GetAliases ()
		{
			return provider.GetAliases ();
		}

		public ColumnName[] GetColumnNames ()
		{
			return columnNameList;
		}

		public int GetColumnOrdinal (ColumnName columnName)
		{
			if (columnNameList == null)
				throw new NotSupportedException (string.Format (
					"Column name {0} not found",
					columnName
				)
				);
			
			return Array.FindIndex (columnNameList, a => a.CompareTo (columnName) == 0);
		}
		
		public Type[] GetColumnTypes ()
		{
			Type[] types = new Type[outputList.Length];
			
			for (int i = 0; i < outputList.Length; i++) {
				types [i] = outputList [i].GetResultType ();
			}
			
			return types;
		}

		public void Initialize (GqlQueryState gqlQueryState)
		{
			this.gqlQueryState = new GqlQueryState (gqlQueryState);

			provider.Initialize (this.gqlQueryState);

			if (outputColumns != null) {
				List<IExpression> outputList = new List<IExpression> ();
				List<ColumnName> columnNameList = new List<ColumnName> ();
				for (int i = 0; i < outputColumns.Count; i++) {
					Column column = outputColumns [i];
					if (column is AllColums) {
						AllColums allColums = (AllColums)column;
						var columnNameList2 = allColums.Provider.GetColumnNames ();
						for (int j = 0; j < columnNameList2.Length; j++) {
							if (allColums.ProviderAlias == null 
								|| StringComparer.InvariantCultureIgnoreCase.Compare (allColums.ProviderAlias, columnNameList2 [j].Alias) == 0) {
								outputList.Add (GqlParser.ConstructColumnExpression (allColums.Provider, j));
								columnNameList.Add (columnNameList2 [j]);
							}
						}
						if (columnNameList.Count == 0)
							throw new InvalidOperationException (string.Format ("No columns found for provider alias {0}", allColums.ProviderAlias));
					} else if (column is SingleColumn) {
						SingleColumn singleColumn = (SingleColumn)column;
						outputList.Add (singleColumn.Expression);
						columnNameList.Add (singleColumn);
					} else {
						throw new InvalidOperationException (string.Format ("Unknown column type {0}", column.GetType ()));
					}
				}
				this.outputList = outputList.ToArray ();
				this.columnNameList = columnNameList.ToArray ();
				for (int i = 0; i < columnNameList.Count; i++) {
					if (this.columnNameList [i] == null) {
						IColumnExpression columnExpression = this.outputList [i] as IColumnExpression;
						if (columnExpression != null) {
							this.columnNameList [i] = columnExpression.ColumnName;
						} else {
							this.columnNameList [i] = new ColumnName (i);
						}
					}
				}
			} else {
				this.outputList = staticOutputList;
				this.columnNameList = staticColumnNameList;
			}

			gqlQueryState.TotalLineNumber = 0;
			record = new ProviderRecord ();
			record.Source = "(subQuery)";
			record.LineNo = 0;
		}

		public bool GetNextRecord ()
		{
			if (!provider.GetNextRecord ())
				return false;
			gqlQueryState.Record = provider.Record;
			gqlQueryState.TotalLineNumber++;
			
			record.Columns = new IComparable[outputList.Length];
			for (int i = 0; i < outputList.Length; i++) {
				record.Columns [i] = outputList [i].EvaluateAsComparable (gqlQueryState);
			}
			
			record.OriginalColumns = provider.Record.Columns;
			record.LineNo = gqlQueryState.TotalLineNumber;
			
			return true;
		}

		public void Uninitialize ()
		{
			record = null;
			gqlQueryState = null;
			if (provider != null)
				provider.Uninitialize ();
			outputList = null;
			columnNameList = null;
		}

		public ProviderRecord Record {
			get {
				return record;
			}
		}
		#endregion

		#region IDisposable implementation
		public void Dispose ()
		{
			if (provider != null)
				provider.Dispose ();
		}
		#endregion
	}
}

