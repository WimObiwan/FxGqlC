using System;

namespace FxGqlLib
{
	public class NamedProvider : IProvider
	{
		IProvider provider;

		public string Name { get; private set; }

		public NamedProvider (IProvider provider, string name)
		{
			this.provider = provider;
			Name = name;
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			provider.Dispose ();
		}
		#endregion

		#region IProvider implementation
		public string[] GetColumnTitles ()
		{
			return provider.GetColumnTitles ();
		}

		public int GetColumnOrdinal (string providerAlias, string columnName)
		{
			if (providerAlias != null && StringComparer.InvariantCultureIgnoreCase.Compare (providerAlias, this.Name) != 0)
				throw new InvalidOperationException (string.Format ("Unexpected provider alias {0} when expecting {1}", providerAlias, this.Name));
			return provider.GetColumnOrdinal (providerAlias, columnName);
		}

		public Type[] GetColumnTypes ()
		{
			return provider.GetColumnTypes ();
		}

		public void Initialize (GqlQueryState gqlQueryState)
		{
			provider.Initialize (gqlQueryState);
		}

		public bool GetNextRecord ()
		{
			return provider.GetNextRecord ();
		}

		public void Uninitialize ()
		{
			provider.Uninitialize ();
		}

		public ProviderRecord Record {
			get {
				return provider.Record;
			}
		}
		#endregion

	}
}

