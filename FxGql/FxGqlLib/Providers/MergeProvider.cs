using System;
using System.Collections.Generic;

namespace FxGqlLib
{
	public class MergeProvider : IProvider
	{
		IList<IProvider> providers;
		int currentProvider;
		GqlQueryState gqlQueryState;
		
		public MergeProvider (IList<IProvider> providers)
		{
			//TODO: Check if providers have compatible Column Types
			//TODO: Check if at least 1 provider is present
			this.providers = providers;
		}
		
		#region IProvider implementation
		public string[] GetColumnTitles ()
		{
			return providers [0].GetColumnTitles ();
		}

		public int GetColumnOrdinal (string columnName)
		{
			return providers [0].GetColumnOrdinal (columnName);
		}
		
		public Type[] GetColumnTypes ()
		{
			return providers [0].GetColumnTypes ();
		}
		
		public void Initialize (GqlQueryState gqlQueryState)
		{
			this.gqlQueryState = gqlQueryState;
			currentProvider = 0;
			providers [0].Initialize (gqlQueryState);
		}

		public bool GetNextRecord ()
		{
			bool result = providers [currentProvider].GetNextRecord ();
			if (result)
				return true;
			if (currentProvider + 1 >= providers.Count)
				return false;
			do {
				providers [currentProvider].Uninitialize ();
				currentProvider++;
				providers [currentProvider].Initialize (gqlQueryState);
				result = providers [currentProvider].GetNextRecord ();
			} while (!result && currentProvider < providers.Count);
				
			return result;
		}

		public void Uninitialize ()
		{
			providers [currentProvider].Uninitialize ();
		}

		public ProviderRecord Record {
			get {
				return providers [currentProvider].Record;
			}
		}
		#endregion

		#region IDisposable implementation
		public void Dispose ()
		{
			foreach (IProvider provider in providers)
				provider.Dispose ();
		}
		#endregion
	}
}

