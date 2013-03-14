using System;
using System.IO;
using System.Globalization;


namespace FxGqlLib
{
	public class UseCommand : IGqlCommand
	{
		readonly FileOptions fileOptions;
		readonly CultureInfo cultureInfo;

		public UseCommand (FileOptions fileOptions, CultureInfo cultureInfo)
		{
			this.fileOptions = fileOptions;
			this.cultureInfo = cultureInfo;
		}

		#region IGqlCommand implementation
		public void Execute (TextWriter outputStream, TextWriter logStream, GqlEngineState gqlEngineState)
		{
			string fileName = fileOptions.FileName.EvaluateAsData (null).ToDataString (cultureInfo);
			gqlEngineState.CurrentDirectory = Path.Combine (gqlEngineState.CurrentDirectory, fileName);
			if (logStream != null)
				logStream.WriteLine ("Current directory changed to '{0}'", gqlEngineState.CurrentDirectory);
		}
		#endregion
	}
}

