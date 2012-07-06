using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace FxGqlLib
{
	public class ColumnProviderRegex : IProvider
	{
		readonly IProvider provider;
		readonly string regexDefinition;
		readonly bool caseInsensitive;

		ProviderRecord record;
		string[] columnNameList;
		Regex regex;
		
		public ColumnProviderRegex (IProvider provider, string regexDefinition, bool caseInsensitive)
		{
			this.provider = provider;
			this.regexDefinition = regexDefinition;
			this.caseInsensitive = caseInsensitive;
		}

		#region IProvider implementation
		public string[] GetColumnTitles ()
		{
			return columnNameList;
		}

		public int GetColumnOrdinal (string providerAlias, string columnName)
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
			regex = new Regex (regexDefinition, caseInsensitive ? RegexOptions.IgnoreCase : RegexOptions.None);
			string[] groups = regex.GetGroupNames ();
			columnNameList = new string[groups.Length - 1];
			Array.Copy (groups, 1, columnNameList, 0, columnNameList.Length);
			for (int i = 0; i < columnNameList.Length; i++)
				if (groups [i + 1] == (i + 1).ToString ())
					columnNameList [i] = "Column" + groups [i + 1];
				else
					columnNameList [i] = groups [i + 1];
			provider.Initialize (gqlQueryState);
						
			record = new ProviderRecord ();
			record.ColumnTitles = columnNameList;
			record.Columns = new string[columnNameList.Length];
			record.OriginalColumns = record.Columns;
		}

		public bool GetNextRecord ()
		{
			while (provider.GetNextRecord ()) {
				string line = provider.Record.Columns [0].ToString ();
				Match match = regex.Match (line);
				if (match.Success) {
					for (int i = 0; i < columnNameList.Length; i++)
						record.Columns [i] = match.Groups [i + 1].Value;
					
					return true;
				}
			}
			
			return false;
		}

		public void Uninitialize ()
		{
			regex = null;
			record = null;
			provider.Uninitialize ();
			columnNameList = null;
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

