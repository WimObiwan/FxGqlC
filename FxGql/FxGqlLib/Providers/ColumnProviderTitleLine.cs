using System;
using System.Linq;

namespace FxGqlLib
{
	public class ColumnProviderTitleLine : IProvider
	{
		IProvider provider;
		GqlEngineState.HeadingEnum heading;
		ColumnName[] columnNameList;

		public ColumnProviderTitleLine (IProvider provider, GqlEngineState.HeadingEnum heading)
		{
			this.provider = provider;
			this.heading = heading;
		}

		#region IProvider implementation
		public string[] GetAliases ()
		{
			return provider.GetAliases ();
		}

		public ColumnName[] GetColumnNames ()
		{
			return columnNameList;
		}

		public int GetColumnOrdinal (ColumnName columnName)
		{
			return Array.FindIndex (columnNameList, a => a.CompareTo (columnName) == 0);
		}
		
		public Type[] GetColumnTypes ()
		{
			Type[] types = new Type[columnNameList.Length];
			for (int i = 0; i < types.Length; i++) { 
				types [i] = typeof(string);
			}
			return types;
		}

		public void Initialize (GqlQueryState gqlQueryState)
		{
			provider.Initialize (gqlQueryState);

			if (heading == GqlEngineState.HeadingEnum.On || heading == GqlEngineState.HeadingEnum.OnWithRule) {
				if (!provider.GetNextRecord ())
					return;

				columnNameList = provider.Record.Columns.Select (p => new ColumnName (p.ToString ())).ToArray ();

				if (heading == GqlEngineState.HeadingEnum.OnWithRule) {
					provider.GetNextRecord ();
				}
			}
		}

		public bool GetNextRecord ()
		{
			if (provider.GetNextRecord ()) {
				Record.OriginalColumns = Record.Columns;
				return true;
			} else {
				return false;
			}
		}

		public void Uninitialize ()
		{
			provider.Uninitialize ();
			columnNameList = null;
		}

		public ProviderRecord Record {
			get {
				return provider.Record;
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


