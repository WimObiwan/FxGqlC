using System;
using System.IO;
using System.Linq;

namespace FxGqlLib
{
	public class MultiFileProvider : IProvider
	{
		IProvider provider;
		string fileMask;
		bool recurse;
		long skip;
		FileOptionsFromClause.FileOrderEnum order;
		StringComparer stringComparer;
		long totalLineNo;

		string[] files;
		GqlQueryState gqlQueryState;
		
		int currentFile;
		
		public MultiFileProvider (string fileMask, bool recurse, long skip, FileOptionsFromClause.FileOrderEnum order, StringComparer stringComparer)
		{
			this.fileMask = fileMask;
			this.recurse = recurse;
			this.skip = skip;
			this.order = order;
			this.stringComparer = stringComparer;
		}

		#region IProvider implementation
		public string[] GetColumnTitles ()
		{
			return new string[] { "Column1" };
		}

		public int GetColumnOrdinal (string columnName)
		{
			return -1;
		}
		
		public Type[] GetColumnTypes ()
		{
			return new Type[] { typeof(string) };
		}
		
		public void Initialize (GqlQueryState gqlQueryState)
		{
			this.gqlQueryState = gqlQueryState;
			string path = Path.GetDirectoryName (fileMask);
			string searchPattern = Path.GetFileName (fileMask);
			SearchOption searchOption;
			if (recurse)
				searchOption = SearchOption.AllDirectories;
			else
				searchOption = SearchOption.TopDirectoryOnly;
			
			path = Path.Combine (gqlQueryState.CurrentDirectory, path); 
			files = Directory.GetFiles (path + Path.DirectorySeparatorChar, searchPattern, searchOption);

			if (order == FileOptionsFromClause.FileOrderEnum.Asc)
				files = files.OrderBy (p => p, stringComparer).ToArray ();
			else if (order == FileOptionsFromClause.FileOrderEnum.Desc)
				files = files.OrderByDescending (p => p, stringComparer).ToArray ();

			currentFile = -1;
			totalLineNo = 0;
			if (!SetNextProvider ())
				throw new FileNotFoundException ("No files found that match with the wildcards", fileMask);
		}

		public bool GetNextRecord ()
		{
			while (!provider.GetNextRecord()) {
				if (!SetNextProvider ()) 
					return false;
			}

			totalLineNo++;
			Record.TotalLineNo = totalLineNo;
			
			return true;
		}

		public void Uninitialize ()
		{
			if (provider != null)
				provider.Uninitialize ();
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

