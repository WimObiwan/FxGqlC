using System;
using System.IO;

namespace FxGqlLib
{
	public class MultiFileProvider : IProvider
	{
		IProvider provider;
		string fileMask;
		bool recurse;
		long skip;
		string[] files;
		
		int currentFile;
		
		public MultiFileProvider (string fileMask, bool recurse, long skip)
		{
			this.fileMask = fileMask;
			this.recurse = recurse;
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
			string path = Path.GetDirectoryName(fileMask);
			string searchPattern = Path.GetFileName(fileMask);
			SearchOption searchOption;
			if (recurse) searchOption = SearchOption.AllDirectories;
			else searchOption = SearchOption.TopDirectoryOnly;

			files = Directory.GetFiles(path + Path.DirectorySeparatorChar, searchPattern, searchOption);
			
			currentFile = -1;
			if (!SetNextProvider())
				throw new FileNotFoundException("No files found that match with the wildcards", fileMask);
		}

		public bool GetNextRecord ()
		{
			while (!provider.GetNextRecord())
			{
				if (!SetNextProvider()) 
					return false;
			}
			
			return true;
		}

		public void Uninitialize ()
		{
			if (provider != null) provider.Uninitialize();
		}

		public ProviderRecord Record {
			get {
				return provider.Record;
			}
		}
		#endregion

		public bool SetNextProvider ()
		{
			if (provider != null)
			{
				provider.Uninitialize();
				provider = null;
			}
			
			currentFile++;
			if (currentFile >= files.Length)
				return false;
			
			provider = FileProviderFactory.Get(files[currentFile], skip);
			provider.Initialize();
			
			return true;
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			if (provider != null) provider.Dispose();
		}
		#endregion
	}
}

