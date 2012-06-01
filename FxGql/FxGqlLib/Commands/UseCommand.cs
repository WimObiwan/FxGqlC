using System;
using System.IO;


namespace FxGqlLib
{
	public class UseCommand : IGqlCommand
	{
		FileOptions fileOptions;

		public UseCommand (FileOptions fileOptions)
		{
			this.fileOptions = fileOptions;
		}

		#region IGqlCommand implementation
		public void Execute (TextWriter outputStream, TextWriter logStream, GqlEngineState gqlEngineState)
		{
			gqlEngineState.CurrentDirectory = Path.Combine (gqlEngineState.CurrentDirectory, fileOptions.FileName);
			if (logStream != null)
				logStream.WriteLine ("Current directory changed to '{0}'", gqlEngineState.CurrentDirectory);
		}
		#endregion
	}
}

