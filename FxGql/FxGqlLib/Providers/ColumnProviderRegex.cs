using System;
using System.Linq;

namespace FxGqlLib
{
	public class ColumnProviderTitleLine : IProvider
	{
		IProvider provider;
		char[] separators;
		ProviderRecord record;
		string[] columns;
		
		public ColumnProviderTitleLine (IProvider provider, char[] separators)
		{
			this.provider = provider;
			this.separators = separators;
		}

		#region IProvider implementation
		public int GetColumnOrdinal(string columnName)
		{
			return Array.FindIndex(columns, a => string.Compare(a, columnName, StringComparison.InvariantCultureIgnoreCase) == 0);
		}
		
		public Type[] GetColumnTypes ()
		{
			Type[] types = new Type[columns.Length];
			for (int i = 0; i < types.Length; i++) { 
				types [i] = typeof(string);
			}
			return types;
		}

		public void Initialize ()
		{
			provider.Initialize ();
			if (provider.GetNextRecord ()) {
				string line = provider.Record.Columns [0].ToString ();
				columns = line.Split (separators, StringSplitOptions.None);
			}
			
			record = new ProviderRecord ();
			record.ColumnTitles = columns;
		}

		public bool GetNextRecord ()
		{
			if (!provider.GetNextRecord ())
				return false;
			
			string line = provider.Record.Columns [0].ToString ();
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

