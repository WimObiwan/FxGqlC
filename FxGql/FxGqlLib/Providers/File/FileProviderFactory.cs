using System;

namespace FxGqlLib
{
	static class FileProviderFactory
	{
		public static IProvider Get (FileOptionsFromClause fileOptions, DataComparer dataComparer)
		{
			string fileName;
			if (fileOptions.FileName is ConstExpression<DataString>)
				fileName = fileOptions.FileName.EvaluateAsData (null).ToDataString (dataComparer.CultureInfo);
			else
				fileName = null;

			IProvider provider;
			if (fileName == null || fileName.Contains ("*") || fileName.Contains ("?") || fileOptions.Recurse) {
				provider = new MultiFileProvider (
					fileOptions,
					dataComparer);
			} else {
				provider = Get (fileName, fileOptions.Skip);
			}
			
			return provider;
		}

		public static IProvider Get (string fileName, long skip)
		{
			IProvider provider;
				
			string extension = System.IO.Path.GetExtension (fileName).ToUpper ();
			switch (extension) {
			case ".ZIP":
			case ".RAR":
			case ".TAR":
			case ".GZIP":
			case ".GZ":
			case ".7Z":
				provider = new ZipFileProvider (fileName, skip);
				break;
			default:
				provider = new FileProvider (fileName, skip);
				break;
			}
			
			return provider;
		}
	}
}

