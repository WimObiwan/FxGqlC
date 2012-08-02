using System;
using System.Linq;

namespace FxGqlLib
{
	public class NamedProvider : IProvider
	{
		readonly IProvider provider;
		readonly string alias;

		public string Alias { get { return alias; } }

		public NamedProvider (IProvider provider, string alias)
		{
			this.provider = provider;
			this.alias = alias;
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			provider.Dispose ();
		}
		#endregion

		#region IProvider implementation
		public string[] GetAliases ()
		{
			return new string[] { Alias };
		}

		public ColumnName[] GetColumnNames ()
		{
			ColumnName[] columnNames = provider.GetColumnNames ();
			if (columnNames != null)
				return columnNames.Select (p => new ColumnName (Alias, p.Name)).ToArray ();
			else
				return null;
		}

		public int GetColumnOrdinal (ColumnName columnName)
		{
			if (columnName.Alias != null) {
				if (StringComparer.InvariantCultureIgnoreCase.Compare (columnName.Alias, this.Alias) != 0)
					//throw new InvalidOperationException (string.Format ("Unexpected provider alias {0} when expecting {1}", columnName.Alias, this.Alias));
					return -1;
				else
					columnName = new ColumnName (null, columnName.Name);
			}

			return provider.GetColumnOrdinal (columnName);
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

