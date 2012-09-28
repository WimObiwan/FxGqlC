using System;
using System.Collections.Generic;
using System.Collections;

namespace FxGqlLib
{
	public class DistinctProvider : IProvider
	{
		readonly IProvider provider;
		readonly DataComparer dataComparer;

		SortedSet<ColumnsComparerKey> recordList;
		ProviderRecord record;

		public DistinctProvider (IProvider provider, DataComparer dataComparer)
		{
			this.provider = provider;
			this.dataComparer = dataComparer;
		}

		#region IProvider implementation
		public string[] GetAliases ()
		{
			return provider.GetAliases ();
		}

		public ColumnName[] GetColumnNames ()
		{
			return provider.GetColumnNames ();
		}

		public int GetColumnOrdinal (ColumnName columnName)
		{
			return provider.GetColumnOrdinal (columnName);
		}
		
		public Type[] GetColumnTypes ()
		{
			return provider.GetColumnTypes ();
		}
		
		public Type[] GetNewColumnTypes ()
		{
			return provider.GetNewColumnTypes ();
		}
		
		public void Initialize (GqlQueryState gqlQueryState)
		{
			provider.Initialize (gqlQueryState);
			ColumnsComparer<ColumnsComparerKey > columnsComparer = new ColumnsComparer<ColumnsComparerKey> (provider.GetColumnTypes (), dataComparer);
			recordList = new SortedSet<ColumnsComparerKey> (columnsComparer);
		}

		public bool GetNextRecord ()
		{
			while (provider.GetNextRecord()) {
				ProviderRecord record = provider.Record;
				ColumnsComparerKey key = new ColumnsComparerKey ();
				key.Members = (IData[])provider.Record.Columns.Clone ();
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

