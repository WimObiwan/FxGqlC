using System;
using System.IO;
using System.Linq;

namespace FxGqlLib
{
	public class MultiFileProvider : IProvider
	{
		readonly FileOptionsFromClause fileOptions;
		readonly StringComparer stringComparer;

		IProvider provider;
		long totalLineNo;
		string[] files;
		GqlQueryState gqlQueryState;
		int currentFile;
		
		public MultiFileProvider (FileOptionsFromClause fileOptions, StringComparer stringComparer)
		{
			this.fileOptions = fileOptions;
			this.stringComparer = stringComparer;
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
			string fileName = fileOptions.FileName.EvaluateAsData (gqlQueryState).ToDataString ();

			this.gqlQueryState = gqlQueryState;
			string path = Path.GetDirectoryName (fileName);
			string searchPattern = Path.GetFileName (fileName);
			SearchOption searchOption;
			if (fileOptions.Recurse)
				searchOption = SearchOption.AllDirectories;
			else
				searchOption = SearchOption.TopDirectoryOnly;
			
			path = Path.Combine (gqlQueryState.CurrentDirectory, path); 
			files = Directory.GetFiles (path + Path.DirectorySeparatorChar, searchPattern, searchOption);

			if (fileOptions.FileOrder == FileOptionsFromClause.FileOrderEnum.Asc)
				files = files.OrderBy (p => p, stringComparer).ToArray ();
			else if (fileOptions.FileOrder == FileOptionsFromClause.FileOrderEnum.Desc)
				files = files.OrderByDescending (p => p, stringComparer).ToArray ();

			currentFile = -1;
			totalLineNo = 0;
			if (!SetNextProvider ())
				throw new FileNotFoundException ("No files found that match with the wildcards", fileName);
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
			
			provider = FileProviderFactory.Get (files [currentFile], fileOptions.Skip);
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

