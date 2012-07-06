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
		GqlEngineExecutionState gqlEngineExecutionState;
		
		public FileProvider (string fileName, long skip)
		{
			this.fileName = fileName;
			this.skip = skip;
		}

		#region IProvider implementation
		public ColumnName[] GetColumnNames ()
		{
			return new ColumnName[] { new ColumnName (null, "Column1") };
		}

		public int GetColumnOrdinal (ColumnName columnName)
		{
			if (StringComparer.InvariantCultureIgnoreCase.Compare (columnName.Name, "Column1") == 0)
				return 0;
			else
				return -1;
		}
		
		public Type[] GetColumnTypes ()
		{
			return new Type[] { typeof(string) };
		}
		
		public void Initialize (GqlQueryState gqlQueryState)
		{
			gqlEngineExecutionState = gqlQueryState.CurrentExecutionState;
			
			string fileName = Path.Combine (
				gqlQueryState.CurrentDirectory,
				this.fileName
			);
			streamReader = new StreamReader (new FileStream (
				fileName,
				FileMode.Open,
				FileAccess.Read,
				FileShare.ReadWrite
			)
			);
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
			record.Columns = new string[] 
			{ 
				text 
			};
			record.OriginalColumns = record.Columns;
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

