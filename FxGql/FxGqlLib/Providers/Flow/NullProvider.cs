using System;

namespace FxGqlLib
{
	public class NullProvider : IProvider
	{
		bool endOfQuery;
		ProviderRecord record;
		
		public NullProvider ()
		{
		}

		#region IProvider implementation
		public string[] GetAliases ()
		{
			return null;
		}

		public ColumnName[] GetColumnNames ()
		{
			return new ColumnName[] { };
		}

		public int GetColumnOrdinal (ColumnName columnName)
		{
			return -1;
		}
		
		public Type[] GetColumnTypes ()
		{
			return new Type[] { };
		}
		
		public void Initialize (GqlQueryState gqlQueryState)
		{
			endOfQuery = false;
			record = new ProviderRecord (this, true);
			record.Source = "(nullProvider)";
			record.LineNo = 0;
		}

		public bool GetNextRecord ()
		{
			if (endOfQuery)
				return false;
			endOfQuery = true;
			return true;
		}

		public void Uninitialize ()
		{
			record = null;
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
		}
		#endregion
	}
}

