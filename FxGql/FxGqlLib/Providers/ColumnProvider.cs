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
	
	public class ColumnProvider : IProvider
	{
		readonly IProvider provider;
		readonly IList<Column> outputColumns;
		readonly IExpression[] staticOutputList;
		readonly string[] staticColumnNameList;

		IExpression[] outputList;
		string[] columnNameList;
		GqlQueryState gqlQueryState;
		ProviderRecord record;

		static IList<Column> ColumnListFromExpressionList (IList<IExpression> expressionList)
		{
			List<Column> columnList = new List<Column> ();
			foreach (IExpression expression in expressionList) {
				Column column = new Column ();
				column.Expression = expression;
				columnList.Add (column);
			}
			return columnList;
		}

		public ColumnProvider (IList<IExpression> outputList, IProvider provider)
			: this(ColumnListFromExpressionList (outputList), provider)
		{
		}

		public ColumnProvider (IList<Column> outputColumns, IProvider provider)
		{
			if (!outputColumns.Any (p => p is AllColums)) {
				if (outputColumns != null) {
					List<IExpression> outputList = new List<IExpression> ();
					List<string> columnNameList = new List<string> ();
					for (int i = 0; i < outputColumns.Count; i++) {
						Column column = outputColumns [i];
						if (column is AllColums) {
							AllColums allColums = (AllColums)column;
							var columnNameList2 = allColums.Provider.GetColumnTitles ();
							for (int j = 0; j < columnNameList2.Length; j++) {
								outputList.Add (GqlParser.ConstructColumnExpression (allColums.Provider, j));
								columnNameList.Add (columnNameList2 [j]);
							}
						} else {
							outputList.Add (column.Expression);
							columnNameList.Add (column.Name);
						}
					}
					this.staticOutputList = outputList.ToArray ();
					this.staticColumnNameList = columnNameList.ToArray ();
					for (int i = 0; i < columnNameList.Count; i++)
						if (this.staticColumnNameList [i] == null)
							this.staticColumnNameList [i] = string.Format ("Column{0}", i + 1);
				}
			} else {
				this.outputColumns = outputColumns;
			}
			this.provider = provider;
		}

		#region IProvider implementation
		public string[] GetColumnTitles ()
		{
			if (columnNameList != null)
				return columnNameList;
			else
				return staticColumnNameList;
		}

		public int GetColumnOrdinal (string columnName)
		{
			string[] columnNameList = GetColumnTitles ();
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
			IExpression[] outputList;
			if (this.outputList != null)
				outputList = this.outputList;
			else
				outputList = staticOutputList;

			Type[] types = new Type[outputList.Length];
			
			for (int i = 0; i < outputList.Length; i++) {
				types [i] = outputList [i].GetResultType ();
			}
			
			return types;
		}

		public void Initialize (GqlQueryState gqlQueryState)
		{
			this.gqlQueryState = new GqlQueryState (gqlQueryState.CurrentExecutionState, gqlQueryState.Variables);
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
							outputList.Add (GqlParser.ConstructColumnExpression (allColums.Provider, j));
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
						IColumnExpression columnExpression = this.outputList [i] as IColumnExpression;
						if (columnExpression != null) {
							this.columnNameList [i] = columnExpression.ColumnName;
						} else {
							this.columnNameList [i] = string.Format ("Column{0}", i + 1);
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

