using System;
using System.IO;
using System.Collections.Generic;

namespace FxGqlLib
{
	public class GqlEngine : IDisposable
	{
		TextWriter outputStream;
		TextWriter logStream;
		TextWriter logFileStream;
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

		string logFile;
		public string LogFile {
			get { return logFile; }
			set {
				if (logFile != value) {
					logFile = value;
					OnLogFileChanged ();
				}
			}
		}

		public GqlEngine ()
		{
			GqlEngineState = new GqlEngineState (gqlEngineExecutionState);
			GqlEngineState.CurrentDirectory = Environment.CurrentDirectory;
			GqlEngineState.TempDirectory = Path.Combine (Path.GetTempPath (), "FxGql-" + Guid.NewGuid ().ToString ());
			Initialize ();
			GqlEngineState.CurrentDirectoryChanged += delegate() {
				ReopenLogFile ();
			};
		}

		private void Initialize ()
		{
			try {
				serial = 0;
				Directory.CreateDirectory (GqlEngineState.TempDirectory);
				OnLogFileChanged ();

				if (logStream != null) {
					logStream.WriteLine (new String ('-', 80));
					logStream.WriteLine ("-- {0} - {1}", serial, GetDate ());
					logStream.WriteLine ("-- FxGql engine started");
				}
			} catch {
			}
		}

		string GetDate ()
		{
			return GetDate (DateTime.Now);
		}
	
		string GetDate (DateTime dateTime)
		{
			return dateTime.ToString ("yyyy-MM-dd HH:mm:ss");
		}
	
		public void Execute (string commandsText)
		{
			gqlEngineExecutionState.InterruptState = GqlEngineExecutionState.InterruptStates.Continue;
			
			serial++;
			if (logStream != null) {
				logStream.WriteLine ("-- {0} - {1}", serial, GetDate ());
				logStream.WriteLine (commandsText);
			}

			GqlParser parser = new GqlParser (GqlEngineState, commandsText);
			IList<IGqlCommand> commands = parser.Parse ();
			
			if (logFileStream != null) {
				logFileStream.WriteLine ("-- {0} - {1}", serial, GetDate ());
				logFileStream.WriteLine (commandsText);
			}

			foreach (IGqlCommand command in commands) {
				command.Execute (outputStream, logStream, GqlEngineState);
			}
		}

		void OnLogFileChanged ()
		{
			ReopenLogFile ();
		}

		void ReopenLogFile ()
		{
			string fullFileName = Path.Combine (GqlEngineState.CurrentDirectory, logFile);
			if (logFileStream != null) {
				logFileStream.Close ();
				logFileStream.Dispose ();
			}
			try {
				logFileStream = new StreamWriter (fullFileName, true);
			} catch {
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
			if (logFileStream != null) {
				logFileStream.Close ();
				logFileStream.Dispose ();
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

