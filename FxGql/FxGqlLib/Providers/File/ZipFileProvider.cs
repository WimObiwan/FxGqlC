using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using System.Collections;

namespace FxGqlLib
{
	public class ZipFileProvider : IProvider
	{
		readonly string fileName;
		readonly long skip;

		ZipFile zipFile;
		long currentFile;
		StreamReader streamReader;
		ProviderRecord record;
		DataString dataString;
		GqlEngineExecutionState gqlEngineExecutionState;
		
		public ZipFileProvider (string fileName, long skip)
		{
			this.fileName = fileName;
			this.skip = skip;
		}

		#region IProvider implementation
		public string[] GetAliases ()
		{
			return null;
		}

		public ColumnName[] GetColumnNames ()
		{
			return new ColumnName[] { new ColumnName (0) };
		}

		public int GetColumnOrdinal (ColumnName columnName)
		{
			if (columnName.CompareTo (new ColumnName (0)) == 0)
				return 0;
			else
				return -1;
		}
		
		public Type[] GetColumnTypes ()
		{
			return new Type[] { typeof(DataString) };
		}
		
		public Type[] GetNewColumnTypes ()
		{
			return new Type[] { typeof(string) };
		}
		
		public void Initialize (GqlQueryState gqlQueryState)
		{
			gqlEngineExecutionState = gqlQueryState.CurrentExecutionState;
			
			string fileName = Path.Combine (gqlQueryState.CurrentDirectory, this.fileName);
			zipFile = new ZipFile (new FileStream (fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 32 * 1024));
			zipFile.UseZip64 = UseZip64.On;
			streamReader = new StreamReader (new AsyncStreamReader (zipFile.GetInputStream (currentFile), 32 * 1024));
			record = new ProviderRecord (this, true);
			record.Source = fileName;
			record.Columns [0] = dataString;
			record.NewColumns [0].Type = typeof(string);
			// same record.OriginalColumns [0] = dataString;

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

			dataString.Set (text);
			record.Columns [0] = dataString;
			record.NewColumns [0].String = text;

			record.LineNo++;
			record.TotalLineNo = record.LineNo;

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


