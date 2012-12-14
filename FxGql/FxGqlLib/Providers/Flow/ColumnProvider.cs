using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FxGqlLib
{
	abstract public class Column : ColumnName
	{
		public Column (ColumnName columnName)
			: base(columnName)
		{
		}

		public Column (string name)
			: base(name)
		{
		}

		public Column (int ordinal)
			: base(ordinal)
		{
		}

		public Column ()
			: base(null, null)
		{
		}
	}

	public class SingleColumn : Column
	{ 
		public SingleColumn (ColumnName columnName, IExpression expr)
			:base(columnName)
		{
			Expression = expr;
		}

		public SingleColumn (string name, IExpression expr)
			:base(name)
		{
			Expression = expr;
		}

		public SingleColumn (int ordinal, IExpression expr)
			:base(ordinal)
		{
			Expression = expr;
		}

		public IExpression Expression { get; private set; }
	}

	public class AllColums : Column
	{
		public AllColums (string providerAlias, IProvider provider)
		{
			ProviderAlias = providerAlias;
			Provider = provider;
		}

		public string ProviderAlias { get; private set; }
		public IProvider Provider { get; private set; }
	}
	
	public class ColumnProvider : IProvider
	{
		readonly IProvider provider;
		readonly IList<Column> outputColumns;
		readonly IExpression[] staticOutputList;
		readonly ColumnName[] staticColumnNameList;

		IExpression[] outputList;
		ColumnName[] columnNameList;
		GqlQueryState gqlQueryState;
		ProviderRecord record;

		static IList<Column> ColumnListFromExpressionList (IList<IExpression> expressionList)
		{
			List<Column> columnList = new List<Column> ();
			for (int i = 0; i < expressionList.Count; i++) {
				IExpression expression = expressionList [i];
				SingleColumn column = new SingleColumn (i, expression);
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
				ConstructColumns (outputColumns, out this.staticOutputList, out this.staticColumnNameList);
			} else {
				this.outputColumns = outputColumns;
			}
			this.provider = provider;
		}

		static void ConstructColumns (IList<Column> outputColumns, out IExpression[] outputList2, out ColumnName[] columnNameList2)
		{
			List<IExpression> outputList = new List<IExpression> ();
			List<ColumnName> columnNameList = new List<ColumnName> ();
			for (int i = 0; i < outputColumns.Count; i++) {
				Column column = outputColumns [i];
				if (column is AllColums) {
					AllColums allColums = (AllColums)column;
					var columnNameList3 = allColums.Provider.GetColumnNames ();
					for (int j = 0; j < columnNameList3.Length; j++) {
						if (allColums.ProviderAlias == null 
							|| StringComparer.InvariantCultureIgnoreCase.Compare (allColums.ProviderAlias, columnNameList3 [j].Alias) == 0) {
							outputList.Add (GqlParser.ConstructColumnExpression (allColums.Provider, j));
							columnNameList.Add (columnNameList3 [j]);
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
			outputList2 = outputList.ToArray ();
			columnNameList2 = columnNameList.ToArray ();
			for (int i = 0; i < columnNameList2.Length; i++)
				if (columnNameList2 [i] == null || string.IsNullOrEmpty (columnNameList2 [i].Name))
					columnNameList2 [i] = new ColumnName (i);
		}

		#region IProvider implementation
		public string[] GetAliases ()
		{
			return provider.GetAliases ();
		}

		public ColumnName[] GetColumnNames ()
		{
			if (columnNameList != null)
				return columnNameList;
			else
				return staticColumnNameList;
		}

		public int GetColumnOrdinal (ColumnName columnName)
		{
			ColumnName[] columnNameList = GetColumnNames ();
			if (columnNameList == null)
				throw new InvalidOperationException (string.Format (
					"Column name {0} not found",
					columnName
				)
				);
			
			return Array.FindIndex (columnNameList, a => a.CompareTo (columnName) == 0);
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

		public Type[] GetNewColumnTypes ()
		{
			IExpression[] outputList;
			if (this.outputList != null)
				outputList = this.outputList;
			else
				outputList = staticOutputList;
			
			Type[] types = new Type[outputList.Length];
			
			for (int i = 0; i < outputList.Length; i++) {
				types [i] = ExpressionBridge.GetNewType (outputList [i].GetResultType ());
			}
			
			return types;
		}
		
		public void Initialize (GqlQueryState gqlQueryState)
		{
			this.gqlQueryState = new GqlQueryState (gqlQueryState);

			provider.Initialize (this.gqlQueryState);

			if (outputColumns != null) {
				ConstructColumns (outputColumns, out this.outputList, out this.columnNameList);
			} else {
				this.outputList = staticOutputList;
				this.columnNameList = staticColumnNameList;
			}

			gqlQueryState.TotalLineNumber = 0;
			record = new ProviderRecord (this, false);
			record.Source = "(subQuery)";
			record.LineNo = 0;
		}

		public bool GetNextRecord ()
		{
			gqlQueryState.TotalLineNumber++;

			do {
				if (!provider.GetNextRecord ())
					return false;
				gqlQueryState.Record = provider.Record;
				gqlQueryState.SkipLine = false;
				for (int i = 0; i < outputList.Length; i++) {
					IData data = outputList [i].EvaluateAsData (gqlQueryState);
					record.Columns [i] = data;
					ExpressionBridge.ConvertFromOld (ref record.NewColumns [i], data);
				}
			} while (gqlQueryState.SkipLine);
			
			record.OriginalColumns = provider.Record.Columns;
			record.NewOriginalColumns = provider.Record.NewColumns;
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

