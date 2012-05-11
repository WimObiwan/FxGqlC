using System;

namespace FxGqlLib
{
	public class FileOptions
	{
		public FileOptions ()
		{
			Recurse = false;
			NewLine = NewLineEnum.Default;
		}
		
		public enum NewLineEnum
		{
			Default,
			Unix,
			Dos,
			Mac
		};
		
		public string FileName { get; set; }

		public bool Recurse { get; set; }

		public NewLineEnum NewLine  { get; set; }

		public bool Append { get; set; }

		public bool Overwrite { get; set; }

		public bool TitleLine { get; set; }
		
		public string ColumnsRegex { get; set; }
		
		public long Skip { get; set; }
	}
	
	static class FileProviderFactory
	{
		public static IProvider Get (FileOptions fileOptions)
		{
			IProvider provider;
			if (fileOptions.FileName.Contains ("*") || fileOptions.FileName.Contains ("?") || fileOptions.Recurse) {
				provider = new MultiFileProvider (fileOptions.FileName, fileOptions.Recurse, fileOptions.Skip);
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

