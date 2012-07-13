using System;
using System.IO;


namespace FxGqlLib
{
	public class UseCommand : IGqlCommand
	{
		readonly FileOptions fileOptions;

		public UseCommand (FileOptions fileOptions)
		{
			this.fileOptions = fileOptions;
		}

		#region IGqlCommand implementation
		public void Execute (TextWriter outputStream, TextWriter logStream, GqlEngineState gqlEngineState)
		{
			string fileName = fileOptions.FileName.EvaluateAsData (null).ToDataString ();
			gqlEngineState.CurrentDirectory = Path.Combine (gqlEngineState.CurrentDirectory, fileName);
			if (logStream != null)
				logStream.WriteLine ("Current directory changed to '{0}'", gqlEngineState.CurrentDirectory);
		}
		#endregion
	}
}

