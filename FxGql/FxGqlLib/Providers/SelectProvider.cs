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
	
	public class SelectProvider : IProvider
	{
		IExpression[] outputList;
		string[] columnNameList;
		IProvider provider;
		GqlQueryState gqlQueryState;
		ProviderRecord record;
		
		public SelectProvider (IList<IExpression> outputList, IProvider provider)
		{
			this.outputList = outputList.ToArray();
			// TODO: Support default columnNames
			this.provider = provider;
		}

		public SelectProvider (IList<Column> outputList, IProvider provider)
		{
			this.outputList = outputList.Select(a => a.Expression).ToArray();
			this.columnNameList = outputList.Select(a => a.Name).ToArray();
			this.provider = provider;
		}

		#region IProvider implementation
		public int GetColumnOrdinal(string columnName)
		{
			if (columnNameList == null)
				throw new NotSupportedException(string.Format("Column name '{0}' not found", columnName));
			
			return Array.FindIndex(columnNameList, a => string.Compare(a, columnName, StringComparison.InvariantCultureIgnoreCase) == 0);
		}
		
		public Type[] GetColumnTypes()
		{
			Type[] types = new Type[outputList.Length];
			
			for (int i = 0; i < outputList.Length; i++) {
				types[i] = outputList[i].GetResultType();
			}
			
			return types;
		}

		public void Initialize ()
		{
			provider.Initialize ();
			gqlQueryState = new GqlQueryState ();
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

