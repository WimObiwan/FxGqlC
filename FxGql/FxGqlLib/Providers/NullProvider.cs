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
		public int GetColumnOrdinal(string columnName)
		{
			return -1;
		}
		
		public Type[] GetColumnTypes()
		{
			return new Type[] { };
		}
		
		public void Initialize ()
		{
			endOfQuery = false;
			record = new ProviderRecord ();
			record.Columns = new IComparable[] { };
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

