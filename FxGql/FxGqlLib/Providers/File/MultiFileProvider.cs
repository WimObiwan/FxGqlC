using System;
using System.IO;
using System.Linq;

namespace FxGqlLib
{
	public class MultiFileProvider : MultiFileProviderBase
	{
		readonly FileOptionsFromClause fileOptions;
		readonly StringComparer stringComparer;

		public MultiFileProvider (FileOptionsFromClause fileOptions, StringComparer stringComparer)
		{
			this.fileOptions = fileOptions;
			this.stringComparer = stringComparer;
		}

		public override void OnInitialize (GqlQueryState gqlQueryState, out string[] files, out long skip)
		{
			string fileName = fileOptions.FileName.EvaluateAsData (gqlQueryState).ToDataString ();

			string path = Path.GetDirectoryName (fileName);
			string searchPattern = Path.GetFileName (fileName);
			SearchOption searchOption;
			if (fileOptions.Recurse)
				searchOption = SearchOption.AllDirectories;
			else
				searchOption = SearchOption.TopDirectoryOnly;
			
			path = Path.Combine (gqlQueryState.CurrentDirectory, path); 
			files = Directory.GetFiles (path + Path.DirectorySeparatorChar, searchPattern, searchOption);

			if (fileOptions.FileOrder == FileOptionsFromClause.FileOrderEnum.Asc 
				|| fileOptions.FileOrder == FileOptionsFromClause.FileOrderEnum.FileNameAsc)
				files = files.Select (p => new FileInfo (p)).OrderBy (p => p.Name, stringComparer).Select (p => p.FullName).ToArray ();
			else if (fileOptions.FileOrder == FileOptionsFromClause.FileOrderEnum.Desc
				|| fileOptions.FileOrder == FileOptionsFromClause.FileOrderEnum.FileNameDesc)
				files = files.Select (p => new FileInfo (p)).OrderByDescending (p => p.Name, stringComparer).Select (p => p.FullName).ToArray ();
			else if (fileOptions.FileOrder == FileOptionsFromClause.FileOrderEnum.ModificationTimeAsc)
				files = files.Select (p => new FileInfo (p)).OrderBy (p => p.LastWriteTime).Select (p => p.FullName).ToArray ();
			else if (fileOptions.FileOrder == FileOptionsFromClause.FileOrderEnum.ModificationTimeDesc)
				files = files.Select (p => new FileInfo (p)).OrderByDescending (p => p.LastWriteTime).Select (p => p.FullName).ToArray ();

			skip = fileOptions.Skip;
		}

		public override void OnUninitialize ()
		{
		}
	}
}

