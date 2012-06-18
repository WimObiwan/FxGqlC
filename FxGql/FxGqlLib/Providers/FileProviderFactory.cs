using System;

namespace FxGqlLib
{
	public class FileOptions
	{
		public FileOptions ()
		{
		}
		
		public string FileName { get; set; }
	}

	public class FileOptionsFromClause : FileOptions
	{
		public FileOptionsFromClause ()
		{
			FileOrder = FileOrderEnum.DontCare;
		}

		public bool Recurse { get; set; }

		public string ColumnsRegex { get; set; }
		
		public GqlEngineState.HeadingEnum Heading { get; set; }
		
		public long Skip { get; set; }

		public enum FileOrderEnum { DontCare, Asc, Desc }
		public FileOrderEnum FileOrder { get; set; }
	}

	public class FileOptionsIntoClause : FileOptions
	{
		public FileOptionsIntoClause ()
		{
			NewLine = NewLineEnum.Default;
		}

		public enum NewLineEnum
		{
			Default,
			Unix,
			Dos,
			Mac
		};

		public NewLineEnum NewLine  { get; set; }

		public bool Append { get; set; }

		public bool Overwrite { get; set; }
	}
	
	static class FileProviderFactory
	{
		public static IProvider Get (FileOptionsFromClause fileOptions, StringComparer stringComparer)
		{
			IProvider provider;
			if (fileOptions.FileName.Contains ("*") || fileOptions.FileName.Contains ("?") || fileOptions.Recurse) {
				provider = new MultiFileProvider (
					fileOptions.FileName,
					fileOptions.Recurse,
					fileOptions.Skip,
					fileOptions.FileOrder,
					stringComparer);
			} else {
				provider = Get (fileOptions.FileName, fileOptions.Skip);
			}
			
			return provider;
		}
		
		public static IProvider Get (string fileName, long skip)
		{
			IProvider provider;
				
			if (System.IO.Path.GetExtension (fileName).ToUpper () == ".ZIP")
				provider = new ZipFileProvider (fileName, skip);
			else
				provider = new FileProvider (fileName, skip);
			
			return provider;
		}
	}
}

