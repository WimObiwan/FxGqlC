using System;
using System.IO;

namespace FxGqlLib
{
	public class FileProvider : IProvider
	{
		readonly string fileName;
		readonly long skip;

		CharArrayReader streamReader;
		//StreamReader streamReader;
		ProviderRecord record;
		GqlEngineExecutionState gqlEngineExecutionState;
		DataString dataString;

		public FileProvider (string fileName, long skip)
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

		public void Initialize (GqlQueryState gqlQueryState)
		{
			gqlEngineExecutionState = gqlQueryState.CurrentExecutionState;

			string fileName;
			if (this.fileName.StartsWith ("#")) {
				fileName = Path.Combine (gqlQueryState.TempDirectory, this.fileName);
			} else {
				fileName = Path.Combine (gqlQueryState.CurrentDirectory, this.fileName);
			}
			streamReader = new CharArrayReader (new StreamReader (new AsyncStreamReader (new FileStream (fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 32 * 1024), 32 * 1024)));
			//streamReader = new StreamReader (new AsyncStreamReader (new FileStream (fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 32 * 1024), 32 * 1024));
			record = new ProviderRecord ();
			record.Source = fileName;
			dataString = new DataString ();
			record.Columns = new IData[] { dataString };
			record.OriginalColumns = record.Columns;

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

			//string text = streamReader.ReadLine ();
			//dataString.Set (text);
			char[] buffer;
			int start, len;
			streamReader.ReadLine (out buffer, out start, out len);
			if (start >= 0)
				dataString.Set (buffer, start, len);
			else
				dataString.Set (null);
			record.Columns [0] = dataString;
			record.LineNo++;
			record.TotalLineNo = record.LineNo;
			
			//return text != null;
			return start >= 0;
		}

		public void Uninitialize ()
		{
			record = null;
			if (streamReader != null) {
				streamReader.Close ();
				streamReader.Dispose ();
			}
			streamReader = null;
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

