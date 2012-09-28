using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace FxGqlLib
{
	public class ColumnProviderDelimiter : IProvider
	{
		readonly protected IProvider provider;
		readonly protected char[] separators;
		readonly int columnCount;

		ProviderRecord record;
		protected ColumnName[] columnNameList;
		protected string[] firstLine;
		DataString[] dataString;

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
				types [i] = typeof(DataString);
			}
			return types;
		}
		
		public Type[] GetNewColumnTypes ()
		{
			Type[] types = new Type[columnNameList.Length];
			for (int i = 0; i < types.Length; i++) { 
				types [i] = typeof(string);
			}
			return types;
		}
		
		protected virtual string[] ReadLine ()
		{
			if (!provider.GetNextRecord ())
				return null;

			string line = provider.Record.Columns [0].ToString ();

			return line.Split (separators, StringSplitOptions.None);
		}

		public virtual void Initialize (GqlQueryState gqlQueryState)
		{
			provider.Initialize (gqlQueryState);
			firstLine = ReadLine ();
			int columns = columnCount;
			if (columns == -1)
				columns = firstLine != null ? firstLine.Length : 0;

			if (columns == -1)
				throw new Exception ("No columns found in delimited file");

			columnNameList = new ColumnName[columns];
			for (int i = 0; i < columnNameList.Length; i++)
				columnNameList [i] = new ColumnName (i);
			dataString = new DataString[columns];

			record = new ProviderRecord (this, false);
		}

		public bool GetNextRecord ()
		{
			string[] line;
			if (firstLine != null) {
				line = firstLine;
				firstLine = null;
			} else {
				line = ReadLine ();
				if (line == null)
					return false;
			}

			for (int i = 0; i < dataString.Length; i++) {
				string text;
				if (i < line.Length)
					text = line [i];
				else
					text = string.Empty;
				dataString [i].Set (text);
				record.Columns [i] = dataString [i];
				record.NewColumns [i].String = text;
			}
			record.LineNo = provider.Record.LineNo;
			record.OriginalColumns = record.Columns;
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

