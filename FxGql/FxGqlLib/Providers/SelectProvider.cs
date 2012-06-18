using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FxGqlLib
{
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
	
	public class SelectProvider : IProvider
	{
		IList<Column> outputColumns;
		IExpression[] outputList;
		string[] columnNameList;
		IProvider provider;
		GqlQueryState gqlQueryState;
		ProviderRecord record;
		
		public SelectProvider (IList<IExpression> outputList, IProvider provider)
		{
			this.outputList = outputList.ToArray ();
			this.columnNameList = new string[outputList.Count];
			for (int i = 0; i < outputList.Count; i++) {
				ColumnExpression columnExpression = outputList [i] as ColumnExpression;
				if (columnExpression != null) {
					this.columnNameList [i] = columnExpression.ColumnName;
				} else {
					this.columnNameList [i] = string.Format ("Column{0}", i + 1);
				}
			}
			this.provider = provider;
		}

		public SelectProvider (IList<Column> outputColumns, IProvider provider)
		{
			/*if (!outputColumns.Any (p => p is AllColums)) {
				if (outputColumns != null) {
					List<IExpression> outputList = new List<IExpression> ();
					List<string> columnNameList = new List<string> ();
					for (int i = 0; i < outputColumns.Count; i++) {
						Column column = outputColumns [i];
						if (column is AllColums) {
							AllColums allColums = (AllColums)column;
							var columnNameList2 = allColums.Provider.GetColumnTitles ();
							for (int j = 0; j < columnNameList2.Length; j++) {
								outputList.Add (new ColumnExpression (allColums.Provider, j));
								columnNameList.Add (columnNameList2 [j]);
							}
						} else {
							outputList.Add (column.Expression);
							columnNameList.Add (column.Name);
						}
					}
					this.outputList = outputList.ToArray ();
					this.columnNameList = columnNameList.ToArray ();
					for (int i = 0; i < columnNameList.Count; i++)
						if (this.columnNameList [i] == null)
							this.columnNameList [i] = string.Format ("Column{0}", i + 1);
				}
			} else*/
			{
				this.outputColumns = outputColumns;
			}
			this.provider = provider;
		}

		#region IProvider implementation
		public string[] GetColumnTitles ()
		{
			return columnNameList;
		}

		public int GetColumnOrdinal (string columnName)
		{
			if (columnNameList == null)
				throw new NotSupportedException (string.Format (
					"Column name '{0}' not found",
					columnName
				)
				);
			
			return Array.FindIndex (
				columnNameList,
				a => string.Compare (
				a,
				columnName,
				StringComparison.InvariantCultureIgnoreCase
			) == 0
			);
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
			this.gqlQueryState = new GqlQueryState (gqlQueryState.CurrentExecutionState);
			this.gqlQueryState.CurrentDirectory = gqlQueryState.CurrentDirectory;
			
			provider.Initialize (this.gqlQueryState);

			if (outputColumns != null) {
				List<IExpression> outputList = new List<IExpression> ();
				List<string> columnNameList = new List<string> ();
				for (int i = 0; i < outputColumns.Count; i++) {
					Column column = outputColumns [i];
					if (column is AllColums) {
						AllColums allColums = (AllColums)column;
						var columnNameList2 = allColums.Provider.GetColumnTitles ();
						for (int j = 0; j < columnNameList2.Length; j++) {
							outputList.Add (new ColumnExpression (allColums.Provider, j));
							columnNameList.Add (columnNameList2 [j]);
						}
					} else {
						outputList.Add (column.Expression);
						columnNameList.Add (column.Name);
					}
				}
				this.outputList = outputList.ToArray ();
				this.columnNameList = columnNameList.ToArray ();
				for (int i = 0; i < columnNameList.Count; i++) {
					if (this.columnNameList [i] == null) {
						ColumnExpression columnExpression = this.outputList [i] as ColumnExpression;
						if (columnExpression != null) {
							this.columnNameList [i] = columnExpression.ColumnName;
						} else {
							this.columnNameList [i] = string.Format ("Column{0}", i + 1);
						}
					}
				}
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
			if (outputColumns != null)
				outputList = null;
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

