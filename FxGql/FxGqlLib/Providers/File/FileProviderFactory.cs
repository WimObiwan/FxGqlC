using System;

namespace FxGqlLib
{
	static class FileProviderFactory
	{
		public static IProvider Get (FileOptionsFromClause fileOptions, StringComparer stringComparer)
		{
			string fileName;
			if (fileOptions.FileName is ConstExpression<DataString>)
				fileName = fileOptions.FileName.EvaluateAsData (null).ToDataString ();
			else
				fileName = null;

			IProvider provider;
			if (fileName == null || fileName.Contains ("*") || fileName.Contains ("?") || fileOptions.Recurse) {
				provider = new MultiFileProvider (
					fileOptions,
					stringComparer);
			} else {
				provider = Get (fileName, fileOptions.Skip);
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

