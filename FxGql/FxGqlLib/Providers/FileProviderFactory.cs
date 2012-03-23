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

		public bool TitleLine { get; set; }
	}
	
	static class FileProviderFactory
	{
		public static IProvider Get (FileOptions fileOptions)
		{
			bool recurse;
			if (fileOptions != null)
				recurse = fileOptions.Recurse;
			else
				recurse = false;
			
			IProvider provider;
			if (fileOptions.FileName.Contains ("*") || fileOptions.FileName.Contains ("?") || recurse) {
				provider = new MultiFileProvider (fileOptions.FileName, recurse);
			} else {
				provider = Get (fileOptions.FileName);
			}
			
			return provider;
		}
		
		public static IProvider Get (string fileName)
		{
			IProvider provider;
				
			if (System.IO.Path.GetExtension (fileName).ToUpper () == ".ZIP")
				provider = new ZipFileProvider (fileName);
			else
				provider = new FileProvider (fileName);
			
			return provider;
		}
	}
}

