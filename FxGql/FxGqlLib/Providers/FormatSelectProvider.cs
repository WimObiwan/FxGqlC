/*
 * using System;

namespace FxGqlLib
{
	public class FormatListSelectProvider : IProvider
	{
		const string separator = "\t";
		
		IProvider provider;
		ProviderRecord record;
		Expression<string> formatListFunction;
		
		public FormatListSelectProvider (IProvider provider)
		{
			this.provider = provider;
		}
		
		#region IProvider implementation
		public void Initialize ()
		{
			provider.Initialize ();
			record = new ProviderRecord ();
			formatListFunction = new FormatListFunction(provider.)
		}

		public bool GetNextRecord ()
		{
			if (!provider.GetNextRecord ())
				return false;
			
			record.LineNo = provider.Record.LineNo;
			record.Source = provider.Record.Source;
			record.OriginalColumns = provider.Record.OriginalColumns;
			
			record.Columns = new string[] 
			{ 
				provider.Record.Columns 
			};
			
			string[] stringArray = new string[provider.Record.Columns.Length * 2 - 1];
			for (int i = 0; i < provider.Record.Columns.Length; i++) {
				if (i > 0)
					stringArray [i * 2 - 1] = separator;
				stringArray [i * 2] = provider.Record.Columns [i].ToString ();
			}
			
			record.Text = string.Concat (stringArray);
			record.LineNo++;
			record.Source = provider.Record.Source;
			
			return true;
		}

		public void Uninitialize ()
		{
			provider.Uninitialize();
			record = null;
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
*/