using System;
using System.IO;

namespace FxGqlLib
{
	public class FileProvider : IProvider
	{
		string fileName;
		long skip;
		StreamReader streamReader;
		ProviderRecord record;
		
		public FileProvider (string fileName, long skip)
		{
			this.fileName = fileName;
			this.skip = skip;
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

			for (long i = 0; i < skip; i++)
			{
				if (streamReader.ReadLine () == null) 
				{
					streamReader.Close();
					streamReader = null;
					return;
				}
			}
		}

		public bool GetNextRecord ()
		{
			if (streamReader == null)
				return false;
			
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

