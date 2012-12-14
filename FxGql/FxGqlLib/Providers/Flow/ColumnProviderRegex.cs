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
		ColumnName[] columnNameList;
		Regex regex;
		DataString[] dataStrings;
		
		public ColumnProviderRegex (IProvider provider, string regexDefinition, bool caseInsensitive)
		{
			this.provider = provider;
			this.regexDefinition = regexDefinition;
			this.caseInsensitive = caseInsensitive;
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
				types [i] = typeof(DataString);
			}
			return types;
		}
		
		public Type[] GetNewColumnTypes ()
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
			columnNameList = groups.Skip (1).Select (p => new ColumnName (p)).ToArray ();
			for (int i = 0; i < columnNameList.Length; i++)
				if (groups [i + 1] == (i + 1).ToString ())
					columnNameList [i] = new ColumnName (i);
				else
					columnNameList [i] = new ColumnName (groups [i + 1]);
			provider.Initialize (gqlQueryState);
						
			record = new ProviderRecord (this, true);
			dataStrings = new DataString[columnNameList.Length];
			for (int i = 0; i < dataStrings.Length; i++) {
				dataStrings [i] = new DataString ();
				record.Columns [i] = dataStrings [i];
				record.NewColumns [i].Type = typeof(string);
			}
		}

		public bool GetNextRecord ()
		{
			while (provider.GetNextRecord ()) {
				string line = provider.Record.Columns [0].ToString ();
				Match match = regex.Match (line);
				if (match.Success) {
					for (int i = 0; i < columnNameList.Length; i++) {
						string text = match.Groups [i + 1].Value;
						dataStrings [i].Set (text);
						record.Columns [i] = dataStrings [i];
						record.NewColumns [i].String = text;
					}
					
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

