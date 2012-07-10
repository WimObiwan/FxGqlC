using System;
using System.Linq;

namespace FxGqlLib
{
	public class ColumnProviderDelimiter : IProvider
	{
		readonly protected IProvider provider;
		readonly protected char[] separators;
		readonly int columnCount;

		// TODO: Cache
		ProviderRecord record;
		protected ColumnName[] columnNameList;
		protected string firstLine;
		
		public ColumnProviderDelimiter (IProvider provider)
			: this(provider, null, -1)
		{
		}

		public ColumnProviderDelimiter (IProvider provider, char[] separators)
			: this(provider, separators, -1)
		{
		}

		public ColumnProviderDelimiter (IProvider provider, char[] separators, int columnCount)
		{
			this.provider = provider;
			if (separators != null)
				this.separators = separators;
			else
				this.separators = new char[] { '\t' };
			this.columnCount = columnCount;
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
			return Array.FindIndex (columnNameList, a => a.CompareTo (columnName) == 0);
		}
		
		public Type[] GetColumnTypes ()
		{
			Type[] types = new Type[columnNameList.Length];
			for (int i = 0; i < types.Length; i++) { 
				types [i] = typeof(string);
			}
			return types;
		}

		public virtual void Initialize (GqlQueryState gqlQueryState)
		{
			provider.Initialize (gqlQueryState);
			if (provider.GetNextRecord ()) {
				firstLine = provider.Record.Columns [0].ToString ();
			} else {
				firstLine = null;
			}

			record = new ProviderRecord ();
			int columns = columnCount;
			if (columns == -1) {
				columns = firstLine.Split (separators).Length;
			}

			if (columns >= 0) {
				columnNameList = new ColumnName[columns];
				for (int i = 0; i < columnNameList.Length; i++)
					columnNameList [i] = new ColumnName (i);
				record.ColumnTitles = columnNameList;
			}
		}

		public bool GetNextRecord ()
		{
			string line;
			if (firstLine != null) {
				line = firstLine;
				firstLine = null;
			} else if (provider.GetNextRecord ()) {
				line = provider.Record.Columns [0].ToString ();
			} else {
				return false;
			}
			
			record.Columns = line.Split (separators, StringSplitOptions.None);
			record.LineNo = provider.Record.LineNo;
			record.OriginalColumns = provider.Record.Columns;
			record.Source = provider.Record.Source;
			return true;
		}

		public void Uninitialize ()
		{
			record = null;
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
			provider.Dispose ();
		}
		#endregion
	}
}

