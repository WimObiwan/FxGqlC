using System;
using System.Collections.Generic;
using System.Collections;

namespace FxGqlLib
{
	public class DistinctProvider : IProvider
	{
		IProvider provider;
		SortedSet<ColumnsComparerKey> recordList;
		ProviderRecord record;
		StringComparer stringComparer;
		
		public DistinctProvider (IProvider provider, StringComparer stringComparer)
		{
			this.provider = provider;
			this.stringComparer = stringComparer;
		}

		#region IProvider implementation
		public Type[] GetColumnTypes ()
		{
			return provider.GetColumnTypes ();
		}
		
		public void Initialize ()
		{
			provider.Initialize ();
			ColumnsComparer<ColumnsComparerKey > columnsComparer = new ColumnsComparer<ColumnsComparerKey> (provider.GetColumnTypes (), stringComparer);
			recordList = new SortedSet<ColumnsComparerKey> (columnsComparer);
		}

		public bool GetNextRecord ()
		{
			while (provider.GetNextRecord()) {
				ProviderRecord record = provider.Record;
				ColumnsComparerKey key = new ColumnsComparerKey ();
				key.Members = provider.Record.Columns;
				if (!recordList.Contains (key)) {
					recordList.Add (key);
					this.record = record;
					return true;
				}
			}
			return false;
		}

		public void Uninitialize ()
		{
			provider.Uninitialize ();
			recordList = null;
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

