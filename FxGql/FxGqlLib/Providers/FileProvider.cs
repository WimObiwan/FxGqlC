using System;
using System.IO;

namespace FxGqlLib
{
	public class FileProvider : IProvider
	{
		string fileName;
		StreamReader streamReader;
		ProviderRecord record;
		
		public FileProvider (string fileName)
		{
			this.fileName = fileName;
		}

		#region IProvider implementation
		public int GetColumnOrdinal(string columnName)
		{
			return -1;
		}
		
		public Type[] GetColumnTypes()
		{
			return new Type[] { typeof(string) };
		}
		
		public void Initialize ()
		{
			streamReader = new StreamReader (new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
			record = new ProviderRecord ();
			record.Source = fileName;
		}

		public bool GetNextRecord ()
		{
			string text = streamReader.ReadLine ();
			record.Columns = new string[] 
			{ 
				text 
			};
			record.OriginalColumns = record.Columns;
			record.LineNo++;
			
			return text != null;
		}

		public void Uninitialize ()
		{
			record = null;
			streamReader.Close ();
			streamReader.Dispose ();
			streamReader = null;
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
			if (streamReader != null)
				streamReader.Dispose ();
		}
		#endregion
	}
}

