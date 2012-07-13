using System;
using System.IO;

namespace FxGqlLib
{
	public class FileProvider : IProvider
	{
		readonly string fileName;
		readonly long skip;

		StreamReader streamReader;
		ProviderRecord record;
		GqlEngineExecutionState gqlEngineExecutionState;
		
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
			streamReader = new StreamReader (new FileStream (
				fileName,
				FileMode.Open,
				FileAccess.Read,
				FileShare.ReadWrite
			)
			);
			record = new ProviderRecord ();
			record.Source = fileName;
			record.Columns = new IData[] { };
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
			
			string text = streamReader.ReadLine ();
			record.Columns [0] = new DataString (text);
			record.LineNo++;
			record.TotalLineNo = record.LineNo;
			
			return text != null;
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

