using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using System.Collections;

namespace FxGqlLib
{
	public class ZipFileProvider : IProvider
	{
		string fileName;
		ZipFile zipFile;
		long currentFile;
		StreamReader streamReader;
		ProviderRecord record;
		
		public ZipFileProvider (string fileName)
		{
			this.fileName = fileName;
		}

		#region IProvider implementation
		public Type[] GetColumnTypes()
		{
			return new Type[] { typeof(string) };
		}
		
		public void Initialize ()
		{
			zipFile = new ZipFile (fileName);
			zipFile.UseZip64 = UseZip64.On;
			streamReader = new StreamReader (zipFile.GetInputStream (currentFile));
			record = new ProviderRecord ();
			record.Source = fileName;
		}

		public bool GetNextRecord ()
		{
			if (streamReader == null)
				return false;

			string text = streamReader.ReadLine ();
			while (text == null) {
				currentFile++;
				if (currentFile >= zipFile.Count)
					return false;
				streamReader = new StreamReader (zipFile.GetInputStream (currentFile));
				text = streamReader.ReadLine ();
			}
			
			record.LineNo++;
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


