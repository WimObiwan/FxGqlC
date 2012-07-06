using System;

namespace FxGqlLib
{
	public class ProviderRecord
	{
		public string[] ColumnTitles { get; set; }

		//public string Text { get; set; }
		public string Source { get; set; }

		public long LineNo { get; set; }

		public long TotalLineNo { get; set; }

		public IComparable[] Columns { get; set; }

		public IComparable[] OriginalColumns { get; set; }
	}
	
	public interface IProvider : IDisposable
	{
		string[] GetColumnTitles ();

		int GetColumnOrdinal (string providerAlias, string columnName);

		Type[] GetColumnTypes ();
		
		void Initialize (GqlQueryState gqlQueryState);

		bool GetNextRecord ();

		ProviderRecord Record { get; }

		void Uninitialize ();
	}
}

