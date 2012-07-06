using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using System.Collections;

namespace FxGqlLib
{
	public class ZipFileProvider : IProvider
	{
		string fileName;
		long skip;
		ZipFile zipFile;
		long currentFile;
		StreamReader streamReader;
		ProviderRecord record;
		GqlEngineExecutionState gqlEngineExecutionState;
		
		public ZipFileProvider (string fileName, long skip)
		{
			this.fileName = fileName;
			this.skip = skip;
		}

		#region IProvider implementation
		public string[] GetColumnTitles ()
		{
			return new string[] { "Column1" };
		}

		public int GetColumnOrdinal (string tableAlias, string columnName)
		{
			return -1;
		}
		
		public Type[] GetColumnTypes ()
		{
			return new Type[] { typeof(string) };
		}
		
		public void Initialize (GqlQueryState gqlQueryState)
		{
			gqlEngineExecutionState = gqlQueryState.CurrentExecutionState;
			
			string fileName = Path.Combine (gqlQueryState.CurrentDirectory, this.fileName);
			zipFile = new ZipFile (fileName);
			zipFile.UseZip64 = UseZip64.On;
			streamReader = new StreamReader (zipFile.GetInputStream (currentFile));
			record = new ProviderRecord ();
			record.Source = fileName;

			for (long i = 0; i < skip; i++) {
				if (streamReader.ReadLine () == null) {
					streamReader.Close ();
					streamReader = null;
					return;
				}
			}
		}

		public bool GetNextRecord ()
		{
			if (gqlEngineExecutionState.InterruptState == GqlEngineExecutionState.InterruptStates.Interrupted)
				throw new InterruptedException ();

			if (streamReader == null)
				return false;
						
			string text = streamReader.ReadLine ();
			while (text == null) {
				currentFile++;
				if (currentFile >= zipFile.Count)
					return false;
				streamReader = new StreamReader (zipFile.GetInputStream (currentFile));

				for (long i = 0; i < skip; i++)
					if (streamReader.ReadLine () == null) 
						return false;
	
				text = streamReader.ReadLine ();
			}
			
			record.LineNo++;
			record.TotalLineNo = record.LineNo;
			record.Columns = new string[] 
			{ 
				text 
			};
			record.OriginalColumns = record.Columns;
			
			return text != null;
		}

		public void Uninitialize ()
		{
			record = null;
			if (zipFile != null) {
				zipFile.Close ();
				zipFile = null;
			}
			if (streamReader != null) {
				streamReader.Close ();
				streamReader.Dispose ();
				streamReader = null;
			}
			gqlEngineExecutionState = null;
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


