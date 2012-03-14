using System;

namespace FxGqlLib
{
	public class MergeProvider : IProvider
	{
		IProvider[] providers;
		int currentProvider;
		
		public MergeProvider (IProvider[] providers)
		{
			this.providers = providers;
		}
		
		#region IProvider implementation
		public void Initialize ()
		{
			currentProvider = 0;
			providers[0].Initialize();
		}

		public bool GetNextRecord ()
		{
			bool result = providers[currentProvider].GetNextRecord();
			if (result) return true;
			if (currentProvider + 1 >= providers.Length) return false;
			do
			{
				providers[currentProvider].Uninitialize();
				currentProvider++;
				providers[currentProvider].Initialize();
				result = providers[currentProvider].GetNextRecord();
			} while (!result && currentProvider < providers.Length);
				
			return result;
		}

		public void Uninitialize ()
		{
			providers[currentProvider].Uninitialize();
		}

		public ProviderRecord Record {
			get {
				return providers[currentProvider].Record;
			}
		}
		#endregion

		#region IDisposable implementation
		public void Dispose ()
		{
			foreach (IProvider provider in providers) provider.Dispose();
		}
		#endregion
	}
}

