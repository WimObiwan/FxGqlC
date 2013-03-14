using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace FxGqlLib
{
	public class GqlEngine : IDisposable
	{
		TextWriter outputStream;
		TextWriter logStream;
		TextWriter logFileStream;
		GqlEngineExecutionState gqlEngineExecutionState = new GqlEngineExecutionState ();
		int serial;
		public Stopwatch ParsingStopWatch { get; private set; }
		public Stopwatch ExecutionStopWatch { get; private set; }

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

		public CultureInfo CultureInfo { get; set; }
		public bool CaseInsensitive { get; set; }

		public GqlEngine ()
		{
			CultureInfo = CultureInfo.InvariantCulture;
			CaseInsensitive = true;
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
				ParsingStopWatch = new Stopwatch ();
				ExecutionStopWatch = new Stopwatch ();
				ParsingStopWatch.Reset ();
				ExecutionStopWatch.Reset ();
				serial = 0;
				Directory.CreateDirectory (GqlEngineState.TempDirectory);
				OnLogFileChanged ();
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
			GqlEngineState.Warnings.Clear ();
			
			serial++;
			if (logStream != null) {
				logStream.WriteLine ("-- {0} - {1}", GetDate (), serial);
				logStream.WriteLine (commandsText);
			}

			GqlParser parser = new GqlParser (GqlEngineState, commandsText, CultureInfo, CaseInsensitive);
			ParsingStopWatch.Start ();
			IList<IGqlCommand> commands = parser.Parse ();
			ParsingStopWatch.Stop ();

			if (logFileStream != null) {
				logFileStream.WriteLine ("-- {0} - {1}", GetDate (), serial);
				logFileStream.WriteLine (commandsText);
				logFileStream.Flush ();
			}

			ExecutionStopWatch.Start ();
			foreach (IGqlCommand command in commands) {
				command.Execute (outputStream, logStream, GqlEngineState);
			}
			ExecutionStopWatch.Stop ();
		}

		void OnLogFileChanged ()
		{
			ReopenLogFile ();
		}

		void ReopenLogFile ()
		{
			CloseLogFile ();
			OpenLogFile ();
		}

		void CloseLogFile ()
		{
			if (logFileStream != null) {
				logFileStream.WriteLine ("-- {0} - FxGql log file closed", GetDate ());
				logFileStream.Close ();
				logFileStream.Dispose ();
				logFileStream = null;
			}
		}

		void OpenLogFile ()
		{
			if (logFile != null) {
				string fullFileName = Path.Combine (GqlEngineState.CurrentDirectory, logFile);
				try {
					logFileStream = new StreamWriter (fullFileName, true);
					logFileStream.WriteLine (new String ('-', 80));
					logFileStream.WriteLine ("-- {0} - FxGql log file opened", GetDate ());
					logFileStream.Flush ();
				} catch {
				}
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
			CloseLogFile ();
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

