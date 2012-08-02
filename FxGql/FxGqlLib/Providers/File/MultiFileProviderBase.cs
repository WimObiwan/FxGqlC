using System;
using System.IO;
using System.Linq;

namespace FxGqlLib
{
	public abstract class MultiFileProviderBase : IProvider
	{
		string[] files;
		long skip;

		IProvider provider;
		long totalLineNo;
		GqlQueryState gqlQueryState;
		int currentFile;
		
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

		public abstract void OnInitialize (GqlQueryState gqlQueryState, out string[] files, out long skip);
		public void Initialize (GqlQueryState gqlQueryState)
		{
			OnInitialize (gqlQueryState, out files, out skip);

			this.gqlQueryState = gqlQueryState;
			currentFile = -1;
			totalLineNo = 0;
			provider = null;
		}

		public bool GetNextRecord ()
		{
			if (provider == null) {
				if (!SetNextProvider ()) 
					return false;
			}

			while (!provider.GetNextRecord()) {
				if (!SetNextProvider ()) 
					return false;
			}

			totalLineNo++;
			Record.TotalLineNo = totalLineNo;
			
			return true;
		}

		public abstract void OnUninitialize ();
		public void Uninitialize ()
		{
			if (provider != null) {
				provider.Uninitialize ();
				provider = null;
			}
			files = null;
			gqlQueryState = null;

			OnUninitialize ();
		}

		public ProviderRecord Record {
			get {
				return provider.Record;
			}
		}
		#endregion

		public bool SetNextProvider ()
		{
			if (provider != null) {
				provider.Uninitialize ();
				provider = null;
			}
			
			currentFile++;
			if (currentFile >= files.Length)
				return false;
			
			provider = FileProviderFactory.Get (files [currentFile], skip);
			provider.Initialize (gqlQueryState);
			
			return true;
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			if (provider != null)
				provider.Dispose ();
		}
		#endregion
	}
}

