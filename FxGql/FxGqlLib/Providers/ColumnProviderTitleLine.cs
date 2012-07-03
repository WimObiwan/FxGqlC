using System;
using System.Linq;

namespace FxGqlLib
{
	public class ColumnProviderTitleLine : IProvider
	{
		IProvider provider;
		GqlEngineState.HeadingEnum heading;
		string[] columnNameList;

		public ColumnProviderTitleLine (IProvider provider, GqlEngineState.HeadingEnum heading)
		{
			this.provider = provider;
			this.heading = heading;
		}

		#region IProvider implementation
		public string[] GetColumnTitles ()
		{
			return columnNameList;
		}

		public int GetColumnOrdinal (string columnName)
		{
			return Array.FindIndex (columnNameList, a => string.Compare (a, columnName, StringComparison.InvariantCultureIgnoreCase) == 0);
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

				columnNameList = provider.Record.Columns.Select (p => p.ToString ()).ToArray ();

				if (heading == GqlEngineState.HeadingEnum.OnWithRule) {
					provider.GetNextRecord ();
				}
			}
		}

		public bool GetNextRecord ()
		{
			return provider.GetNextRecord ();
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


