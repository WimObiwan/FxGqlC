using System;
using System.IO;
using System.Collections.Generic;

namespace FxGqlLib
{
	public class GqlEngine : IDisposable
	{
		TextWriter outputStream;
		TextWriter logStream;
		GqlEngineExecutionState gqlEngineExecutionState = new GqlEngineExecutionState ();
		int serial;

		public GqlEngineState GqlEngineState { get; private set; }
		
		public TextWriter OutputStream {
			get { return outputStream; }
			set { outputStream = value; }
		}

		public TextWriter LogStream {
			get { return logStream; }
			set { logStream = value; }
		}

		public GqlEngine ()
		{
			GqlEngineState = new GqlEngineState (gqlEngineExecutionState);
			GqlEngineState.CurrentDirectory = Environment.CurrentDirectory;
			GqlEngineState.TempDirectory = Path.Combine (Path.GetTempPath (), "FxGql-" + Guid.NewGuid ().ToString ());
			Initialize ();
		}

		private void Initialize ()
		{
			try {
				serial = 0;
				Directory.CreateDirectory (GqlEngineState.TempDirectory);
			} catch {
			}
		}
	
		public void Execute (string commandsText)
		{
			gqlEngineExecutionState.InterruptState = GqlEngineExecutionState.InterruptStates.Continue;
			
			serial++;
			if (logStream != null) {
				logStream.WriteLine ("-- {0} - {1}", serial, DateTime.Now.ToString ("o"));
				logStream.WriteLine (commandsText);
			}
			GqlParser parser = new GqlParser (GqlEngineState, commandsText);
			IList<IGqlCommand> commands = parser.Parse ();
			
			foreach (IGqlCommand command in commands) {
				command.Execute (outputStream, logStream, GqlEngineState);
			}
		}

		private void Uninitialize ()
		{
			GqlEngineState.Variables.Clear ();
			GqlEngineState.Views.Clear ();
			if (Directory.Exists (GqlEngineState.TempDirectory)) {
				try {
					Directory.Delete (GqlEngineState.TempDirectory, true);
				} catch {
				}
			}
		}

		public void Reset ()
		{
			Uninitialize ();
			Initialize ();
		}		

		public void Interrupt ()
		{
			gqlEngineExecutionState.InterruptState = GqlEngineExecutionState.InterruptStates.Interrupted;
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			Uninitialize ();
		}
		#endregion

	}
}

