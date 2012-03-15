using System;
using System.Collections.Generic;

namespace FxGqlLib
{
	public class DistinctProvider : IProvider
	{
		IProvider provider;
		SortedSet<Key> recordList;
		ProviderRecord record;
		// TODO: Support for Distinct case insensitive
		//IComparer<string> comparer;

		class Key : IComparable<Key>
		{
			public IComparable[] Members { get; set; }
			
			#region IComparable[Key] implementation
			public int CompareTo (Key other)
			{
				for (int i = 0; i < Members.Length; i++)
				{
					int result = this.Members[i].CompareTo(other.Members[i]);
					if (result != 0) return result;
				}
				
				return 0;
			}
			#endregion
		}
		
		public DistinctProvider (IProvider provider, IComparer<string> comparer)
		{
			this.provider = provider;
			//this.comparer = comparer;
		}

		#region IProvider implementation
		public void Initialize ()
		{
			provider.Initialize();
			recordList = new SortedSet<Key>();
		}

		public bool GetNextRecord ()
		{
			while (provider.GetNextRecord())
			{
				ProviderRecord record = provider.Record;
				Key key = new Key();
				key.Members = provider.Record.Columns;
				if (!recordList.Contains(key))
				{
					recordList.Add(key);
					this.record = record;
					return true;
				}
			}
			return false;
		}

		public void Uninitialize ()
		{
			provider.Uninitialize();
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
			provider.Dispose();
		}
		#endregion
	}
}

