using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using FxGqlLib;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Diagnostics;

namespace FxGqlC
{
	static class MainClass
	{
		static GqlEngine gqlEngine;
		static string version;
		static int versionType;
		static string lastRelease;
		static bool nochecknewversion = false;
		static bool noautoupdate = false;
		static bool notracking = false;
		//static DateTime lastCheck = DateTime.MinValue;
		static bool continuePromptMode = true;
		static bool verbose = false;
		static bool autoSize = false;
		static int autoSizeRows = -1;

		static int uniqueVisitorId = GetUniqueId ();
		
		static int updatesBusy = 0;
		static ManualResetEvent manualResetEvent = new ManualResetEvent (false);

		[Flags]
		enum ReportError
		{
			ApplicationExceptions = 0x1,
			ExecutionExceptions = 0x2,
			ParsingExceptions = 0x4,
			NoConfirm = 0x8000,
			None = 0, 
			All = 0x7fff,
			Auto = ApplicationExceptions,
		};
		static ReportError reportError = ReportError.Auto;

		enum OnOffEnum
		{
			Default =0,
			On = 1,
			Off = 2,
		}

		static string GetVersion ()
		{
			var info = System.Diagnostics.FileVersionInfo.GetVersionInfo (Assembly.GetExecutingAssembly ().Location);
			string type;
			versionType = info.FileBuildPart;
			switch (versionType) {
			case 0:
				type = "alpha";
				break;
			case 1:
				type = "beta";
				break;
			case 2:
				type = "rc";
				break;
			case 3:
				type = (info.FilePrivatePart == 0) ? null : "r";
				break;
			default:
				type = "";
				break;
			}

			string version;
			if (type != null)
				version = string.Format ("v{0}.{1}.{2}{3}", info.FileMajorPart, info.FileMinorPart, type, info.FilePrivatePart);
			else
				version = string.Format ("v{0}.{1}", info.FileMajorPart, info.FileMinorPart);

			version += "-" + RetrieveLinkerTimestamp ().ToString ("yyyyMMdd");

			return version;
		}

		[STAThread]
		public static void Main (string[] args)
		{
			// Check for updates
			version = GetVersion ();
			CheckForUpdates (State.Start);

			bool nologo = false;
			bool help = false;
			bool license = false;
			bool prompt = false;
			string command = null;
			string gqlFile = null;
			string logFile = "log.gql";
			string autoexec = null;
			List<string> errors = new List<string> ();
			
			for (int i = 0; i < args.Length; i++) {
				if (string.Equals (args [i], "-nologo", StringComparison.InvariantCultureIgnoreCase))
					nologo = true;
				else if (string.Equals (args [i], "-help", StringComparison.InvariantCultureIgnoreCase)
					|| string.Equals (args [i], "-h", StringComparison.InvariantCultureIgnoreCase))
					help = true;
				else if (string.Equals (args [i], "-license", StringComparison.InvariantCultureIgnoreCase))
					license = true;
				else if (string.Equals (args [i], "-prompt", StringComparison.InvariantCultureIgnoreCase)
					|| string.Equals (args [i], "-p", StringComparison.InvariantCultureIgnoreCase))
					prompt = true;
				else if (string.Equals (args [i], "-command", StringComparison.InvariantCultureIgnoreCase)
					|| string.Equals (args [i], "-c", StringComparison.InvariantCultureIgnoreCase)) {
					i++;
					if (i < args.Length)
						command = args [i];
					else
						errors.Add ("Please specify a GQL command after '-command'");
				} else if (string.Equals (args [i], "-gqlfile", StringComparison.InvariantCultureIgnoreCase)) {
					i++;
					if (i < args.Length)
						gqlFile = args [i];
					else
						errors.Add ("Please specify a GQL file after '-gqlfile'");
				} else if (string.Equals (args [i], "-logfile", StringComparison.InvariantCultureIgnoreCase)) {
					if (i + 1 < args.Length && !args [i + 1].StartsWith ("-")) {
						i++;
						logFile = args [i];
					} else {
						logFile = null;
					}
				} else if (string.Equals (args [i], "-autoexec", StringComparison.InvariantCultureIgnoreCase)) {
					i++;
					if (i < args.Length)
						autoexec = args [i];
					else
						errors.Add ("Please specify a GQL file after '-autoexec'");
				} else if (string.Equals (args [i], "-nochecknewversion", StringComparison.InvariantCultureIgnoreCase)) {
					nochecknewversion = true;
					notracking = true;
				} else if (string.Equals (args [i], "-nochecknewversion", StringComparison.InvariantCultureIgnoreCase)) {
					noautoupdate = true;
				} else {
					errors.Add (string.Format ("Unknown parameter '{0}'", args [i]));
				}				
			}
						
			if (!nologo) {
				var info = System.Diagnostics.FileVersionInfo.GetVersionInfo (Assembly.GetExecutingAssembly ().Location);

				Console.WriteLine ();
				Console.WriteLine ("{0} - {1} - {2}", info.FileDescription, version, info.Comments);				
				Console.WriteLine (info.LegalCopyright);
			}
							
			if (license) {
				Console.WriteLine ("This program is free software: you can redistribute it and/or modify");
				Console.WriteLine ("it under the terms of the GNU General Public License as published by");
				Console.WriteLine ("the Free Software Foundation, either version 3 of the License, or");
				Console.WriteLine ("any later version.");
				Console.WriteLine ();
				Console.WriteLine ("This program is distributed in the hope that it will be useful,");
				Console.WriteLine ("but WITHOUT ANY WARRANTY; without even the implied warranty of");
				Console.WriteLine ("MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the");
				Console.WriteLine ("GNU General Public License for more details.");
				Console.WriteLine ();
				Console.WriteLine ("You should have received a copy of the GNU General Public License");
				Console.WriteLine ("along with this program.  If not, see <http://www.gnu.org/licenses/>.");
				Console.WriteLine ();
				Console.WriteLine ("FxGqlC depends on (and/or contains redistributables of) these open source");
				Console.WriteLine ("products:");
				Console.WriteLine ("* SharpZipLib, licensed under GPLv2, Copyright 2001-2010 Mike Krueger, ");
				Console.WriteLine ("  John Reilly.");
				Console.WriteLine ("* Antlr v3, Antlr3 license (BSD), Copyright (c) 2010 Terence Parr.");
				Console.WriteLine ("* Mono MCS getline.cs, MIT X11 / Apache License 2.0A,  Copyright 2008 Novell.");
				Console.WriteLine ();
				Console.WriteLine ("Contact Information: Wim Devos, wim AT obiwan DOT be");
				Console.WriteLine ("===========================================================================");
				return;
			} else if (!nologo) {
				Console.WriteLine ("Distributed under GPLv3.  Run FxGqlC.exe -license for more information.");
				Console.WriteLine ("===========================================================================");
			}

			if (help || errors.Count > 0) {
				foreach (string error in errors) {
					Console.WriteLine (error);
				}
					
				ShowHelp ();
				return;
			}
			
			Console.TreatControlCAsInput = false;
			Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs e) {
				if (e.SpecialKey == ConsoleSpecialKey.ControlC) {
					//Console.WriteLine ("===== Ctrl-C signal received =====");
					e.Cancel = true;
					gqlEngine.Interrupt ();
				}
			};

			using (gqlEngine = new GqlEngine ()) {
				gqlEngine.OutputStream = Console.Out;
				gqlEngine.LogFile = logFile;
//				if (logFile != null) {
//					try {
//						//gqlEngine.LogStream = new StreamWriter (logFile, true);
//					} catch (Exception x) {
//						Console.WriteLine ("Unable to open logfile '{0}'.  Continuing without logfile.", logFile);
//						Console.WriteLine (x.Message);
//					}
//				}

				try {
					if (autoexec != null) {
						ExecuteFile (autoexec);
					} else {
						if (File.Exists ("autoexec.gql"))
							ExecuteFile ("autoexec.gql");
						else {
							string path = Path.GetDirectoryName (new Uri (
							Assembly.GetAssembly (typeof(MainClass)).CodeBase).LocalPath
							);
							if (File.Exists (Path.Combine (path, "autoexec.gql")))
								ExecuteFile (Path.Combine (path, "autoexec.gql"));
						}
					}
				} catch (Exception x) {
					Console.WriteLine ("Failed to execute autoexec script: {0}", x.Message);
					ReportException ("autoexec", x);
				}

				try {
					if (prompt) {
						RunPrompt ();
					} else if (command != null) {
						ExecuteCommand (command);
					} else if (gqlFile != null) {
						ExecuteFile (gqlFile);
					} else {
						ShowHelp ();
					}
				} finally {
					if (gqlEngine.LogStream != null) {
						gqlEngine.LogStream.Close ();
						gqlEngine.LogStream.Dispose ();
						gqlEngine.LogStream = null;
					}
				}			
			}

			CheckForUpdates (State.Stop);
			manualResetEvent.WaitOne (500);
		}

		static private DateTime RetrieveLinkerTimestamp ()
		{
			string filePath = System.Reflection.Assembly.GetCallingAssembly ().Location;
			const int c_PeHeaderOffset = 60;
			const int c_LinkerTimestampOffset = 8;
			byte[] b = new byte[2048];
			System.IO.Stream s = null;

			try {
				s = new System.IO.FileStream (filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
				s.Read (b, 0, 2048);
			} finally {
				if (s != null) {
					s.Close ();
				}
			}

			int i = System.BitConverter.ToInt32 (b, c_PeHeaderOffset);
			int secondsSince1970 = System.BitConverter.ToInt32 (b, i + c_LinkerTimestampOffset);
			DateTime dt = new DateTime (1970, 1, 1, 0, 0, 0);
			dt = dt.AddSeconds (secondsSince1970);
			dt = dt.AddHours (TimeZone.CurrentTimeZone.GetUtcOffset (dt).Hours);
			return dt;
		}

		public static void ShowHelp ()
		{
			Console.WriteLine ("FxGqlC.exe usage");
			Console.WriteLine ("   -help or -h         Show this help");
			Console.WriteLine ("   -license            Shows license information");
			Console.WriteLine ("   -prompt or -p       Run in prompt mode");
			Console.WriteLine ("   -command <command>  Run a single command (usefull when running FxGqlC in");
			Console.WriteLine ("        or -c <cmd>         scripts)");
			Console.WriteLine ("   -gqlfile <file>     Run the commands from a file");
			Console.WriteLine ("   -logfile <file>     Outputs all GQL commands and query output to a file");
			Console.WriteLine ("   -autoexec <file>    Runs this file instead of 'autoexec.gql' before");
			Console.WriteLine ("                            running any other command or before command ");
			Console.WriteLine ("                            prompt mode is started");
			//Console.WriteLine ("   -loglevel <#>")
			Console.WriteLine ("===========================================================================");
		}

		static string MakeRelative (string currentDirectory, string file)
		{
			Uri uriCurrentDirectory = new Uri (currentDirectory + "/");
			Uri uriFile = new Uri (file);
			return Uri.UnescapeDataString (uriCurrentDirectory.MakeRelativeUri (uriFile).ToString ());
		}

		public static void RunPrompt ()
		{
			Mono.Terminal.LineEditor lineEditor = new Mono.Terminal.LineEditor ("FxGqlC", 50);
			lineEditor.CtrlOPressed += delegate(object sender, EventArgs args) {
				//var copy = Console.Error;
				//Console.SetError (TextWriter.Null);
				string currentDirectory = gqlEngine.GqlEngineState.CurrentDirectory;
				string[] files = FxGqlCWin.FileSelector.SelectMultipleFileRead ("Select file(s) for the FROM clause", currentDirectory);
				//Console.SetError (copy);
				if (files != null) {
					StringBuilder sb = new StringBuilder ();
					foreach (string file in files) {
						string relativeFile = MakeRelative (currentDirectory, file);
						if (sb.Length > 0)
							sb.Append (", ");
						sb.AppendFormat ("['{0}']", relativeFile);
					}
					lineEditor.Type (sb.ToString ());
				}
			};

			while (continuePromptMode) {
				string command = lineEditor.Edit ("FxGqlC> ", "");
				//Console.Write ("FxGqlC> ");
				//string command = Console.ReadLine ();
				if (command.Trim ().Equals ("exit", StringComparison.InvariantCultureIgnoreCase)
					|| command.Trim ().Equals ("quit", StringComparison.InvariantCultureIgnoreCase))
					command = "!!exit"; 
				if (!ExecutePromptCommand (command, lineEditor))
				if (!ExecuteAliasCommand (command))
					ExecuteCommand (command);
			}
			lineEditor.Close ();
		}

		static bool ExecutePromptCommand (string command, Mono.Terminal.LineEditor lineEditor)
		{
			command = command.Trim ();
			if (command.StartsWith ("!!")) {
				command = command.Substring (2).TrimStart ();
				lineEditor.RemoveLast ();
				switch (command.ToUpper ()) {
				case "SHOWHISTORY":
					lineEditor.ShowHistory ();
					break;
				case "EXIT":
				case "QUIT":
					continuePromptMode = false;
					break;
				default:
					Console.WriteLine ("Unknown prompt command '{0}'", command);
					break;
				}
				return true;
			} else {
				return false;
			}
		}

		public static IEnumerable<string> SplitCommandLine (string commandLine)
		{
			bool inQuotes = false;

			return commandLine.Split (c =>
			{
				if (c == '\"')
					inQuotes = !inQuotes;

				return !inQuotes && c == ' ';
			}
			)
                          .Select (arg => arg.Trim ().TrimMatchingQuotes ('\"'))
                          .Where (arg => !string.IsNullOrEmpty (arg));
		}

		static string EmptyToNull (string value)
		{
			return string.IsNullOrEmpty (value) ? null : value;
		}

		static string GetValue (string[] components, string id)
		{
			int id2 = int.Parse (id);
			return id2 > 0 && id2 < components.Length ? EmptyToNull (components [id2]) : null;

		}

		static bool ExecuteAliasCommand (string command)
		{
			command = command.Trim ();
			if (command.StartsWith ("@")) {
				command = command.Substring (1).TrimStart ();
				string[] components = SplitCommandLine (command).ToArray ();
				string definition;
				if (aliases.TryGetValue (components [0], out definition)) {
					definition = Regex.Replace (definition, @"(?:\$\((?<id>\d+)(?:,(?<def>[^\)]+))?\))|(?:\$(?<id>\d+))", 
					                            m => GetValue (components, m.Groups ["id"].Value) ?? EmptyToNull (m.Groups ["def"].Value) ?? "");
					ExecuteCommand (definition);
				} else {
					Console.WriteLine ("Unknown command alias '{0}'", command);
				}
				return true;
			} else {
				return false;
			}
		}

		public static void ExecuteCommand (string command)
		{
			if (!ExecuteClientCommand (command)) {
				ExecuteServerCommand (command);
			}
			CheckToDisplayNewVersionMessage ();
		}

		static bool ExecuteClientCommand (string command)
		{
			command = command.Trim ();
			if (command.StartsWith ("!")) {
				command = command.Substring (1).TrimStart ();
				string[] commandComponents = command.Split (new char[] {' '}, 2, StringSplitOptions.RemoveEmptyEntries);

				if (commandComponents.Length < 1) {
					Console.WriteLine ("Invalid client command syntax");
				} else {
					switch (commandComponents [0].ToUpperInvariant ()) {
					case "SET":
						ExecuteClientCommandSet (commandComponents [1]);
						break;
					case "EXECUTE":
						ExecuteClientCommandExecute (commandComponents [1]);
						break;
					case "ALIAS":
						ExecuteClientCommandAlias (commandComponents [1]);
						break;
					default:
						Console.WriteLine ("Unknown client command '{0}'", commandComponents [0]);
						break;
					}
				}
				return true;
			} else {
				return false;
			}
		}

		public static void ExecuteServerCommand (string command)
		{
#if DEBUG
			gqlEngine.Execute (command);
#else
			try {
				gqlEngine.Execute (command);

				foreach (Exception x in gqlEngine.GqlEngineState.Warnings) {
					if (verbose)
						Console.WriteLine ("WARNING: {0}", x);
					else
						Console.WriteLine ("WARNING: {0}", x.Message);
					if (gqlEngine.LogStream != null) 
						gqlEngine.LogStream.WriteLine (x.ToString ());
				}
			} catch (FxGqlLib.ParserException x) {
				if (verbose)
					Console.WriteLine (x);
				else
					Console.WriteLine (x.Message);
				if (gqlEngine.LogStream != null) 
					gqlEngine.LogStream.WriteLine (x.ToString ());

				string line;
				using (StringReader stringReader = new System.IO.StringReader(command)) {
					for (int no = 0; (line = stringReader.ReadLine()) != null; no++) {
						Console.WriteLine ("{0,3}: {1}", no + 1, line);
						if (gqlEngine.LogStream != null) 
							gqlEngine.LogStream.WriteLine ("{0,3}: {1}", no + 1, line);
						
						if (no + 1 == x.Line) {
							Console.WriteLine ("     {0}^", new string (' ', Math.Max (0, x.Pos)));
							if (gqlEngine.LogStream != null) 
								gqlEngine.LogStream.WriteLine ("     {0}^", new string (' ', Math.Max (0, x.Pos)));
						}
					}
				}
				ReportException ("executing server command, " + command, x);
			} catch (Exception x) {
				if (verbose)
					Console.WriteLine (x);
				else
					Console.WriteLine (x.Message);
				if (gqlEngine.LogStream != null) 
					gqlEngine.LogStream.WriteLine (x.ToString ());
				ReportException ("executing server command, " + command, x);
			}
#endif
		}

		public static void ExecuteFile (string file)
		{
			using (StreamReader reader = new StreamReader(file)) {
				string command = reader.ReadToEnd ();
				ExecuteCommand (command);
			}
		}

		static void ExecuteClientCommandSet (string command)
		{
			string[] commandComponents = command.Split (new char[] {' '}, 2, StringSplitOptions.RemoveEmptyEntries);
			if (commandComponents.Length < 2) {
				Console.WriteLine ("Invalid number of components in client command 'SET'");
			} else {
				string key = commandComponents [0];
				string value = commandComponents [1];
				switch (key.ToUpperInvariant ()) {
				case "HEADING":
					{
						GqlEngineState.HeadingEnum heading;
						if (Enum.TryParse<GqlEngineState.HeadingEnum> (value, true, out heading)) 
							gqlEngine.GqlEngineState.Heading = heading;
						else
							Console.WriteLine ("Unknown SET HEADING value '{0}'", value);

						break;
					}
				case "REPORTERROR":
					{
						ReportError reportError;
						if (Enum.TryParse<ReportError> (value, true, out reportError)) 
							MainClass.reportError = reportError;
						else
							Console.WriteLine ("Unknown SET REPORTERROR value '{0}'", value);

						break;
					}
				case "VERBOSE":
					{
						OnOffEnum onOff;
						if (Enum.TryParse<OnOffEnum> (value, true, out onOff)) 
							verbose = (onOff == OnOffEnum.On);
						else
							Console.WriteLine ("Unknown SET VERBOSE value '{0}'", value);
						break;
					}
				case "AUTOSIZE":
					{
						OnOffEnum onOff;
						if (Enum.TryParse<OnOffEnum> (value, true, out onOff)) {
							autoSize = (onOff == OnOffEnum.On);
							if (autoSize) 
								gqlEngine.GqlEngineState.AutoSize = autoSizeRows;
							else
								gqlEngine.GqlEngineState.AutoSize = 0;
						} else {
							Console.WriteLine ("Unknown SET AUTOSIZE value '{0}'", value);
						}
						break;
					}
				case "AUTOSIZEROWS":
					{
						if (int.TryParse (value, out autoSizeRows)) {
							if (autoSize) 
								gqlEngine.GqlEngineState.AutoSize = autoSizeRows;
							else
								gqlEngine.GqlEngineState.AutoSize = 0;
						} else {
							Console.WriteLine ("Unknown SET AUTOSIZEROWS value '{0}'", value);
						}
						break;
					}
				case "COLUMNDELIMITER":
					if (value.Length >= 3 && value.StartsWith ("\'") && value.EndsWith ("\'"))
						value = value.Substring (1, value.Length - 2);
					gqlEngine.GqlEngineState.ColumnDelimiter = Regex.Unescape (value);
					break;
				default:
					Console.WriteLine ("Unknown SET command '{0}'", key);
					break;
				}
			}
		}

		static int fileExecutionDepth = 0;

		static void ExecuteClientCommandExecute (string file)
		{
			try {
				fileExecutionDepth++;

				int maxFileExecutionDepth = 16;
				if (fileExecutionDepth > maxFileExecutionDepth)
					throw new InvalidOperationException ("Maximum file invocation depth (" + maxFileExecutionDepth.ToString () + ") reached.");

				if (file.StartsWith ("[") && file.EndsWith ("]"))
					file = file.Substring (1, file.Length - 2);
				if (file.StartsWith ("'") && file.EndsWith ("'"))
					file = file.Substring (1, file.Length - 2);

				ExecuteFile (file);
			} finally {
				fileExecutionDepth--;
			}
		}

		static Dictionary<string, string> aliases = new Dictionary<string, string> ();

		static void ExecuteClientCommandAlias (string command)
		{
			string[] commandComponents = command.Split (new char[] {' '}, 2, StringSplitOptions.RemoveEmptyEntries);
			if (commandComponents.Length < 2) {
				Console.WriteLine ("Invalid number of components in client command 'ALIAS'");
			} else {
				string alias = commandComponents [0];
				string definition = commandComponents [1];

				aliases [alias] = definition;
			}
		}

		enum State
		{
			Start,
			Continue,
			Stop
		}

		static void CheckForUpdates (State state)
		{
			if (nochecknewversion && notracking)
				return;
			//DateTime now = DateTime.Now;
			//if (lastCheck == DateTime.MinValue || lastCheck + new TimeSpan (0, 15, 0) < now) {
			//	lastCheck = now;
			
			System.Threading.Interlocked.Increment (ref updatesBusy);
			manualResetEvent.Reset ();
			
			System.Threading.WaitCallback waitCallback = new System.Threading.WaitCallback (delegate(object state2) {
				CheckForUpdatesAsync (state);
			}
			);
			System.Threading.ThreadPool.QueueUserWorkItem (waitCallback);
		}

		static void CheckForUpdatesAsync (State state)
		{
			if (nochecknewversion && notracking)
				return;
			for (int i = 0; i < 3; i++) {
				try {
					System.Net.ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback (delegate(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors) {
						return true;
					}
					);

					using (var client = new System.Net.WebClient ()) {

						string culture = System.Threading.Thread.CurrentThread.CurrentCulture.Name;
//					string os;
//					os = System.Text.RegularExpressions.Regex.Replace (Environment.OSVersion.VersionString, @"^.*(Windows NT \d+\.\d+).*$", "$1");
//					//client.Headers.Add (System.Net.HttpRequestHeader.UserAgent, "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 7.1; Trident/5.0)");
//					client.Headers.Add (System.Net.HttpRequestHeader.UserAgent, "Mozilla/5.0 (compatible; MSIE 9.0; " + os + "; Trident/5.0)");
//					//client.Headers ["user-agent"] = "Mozilla/5.0 (compatible; MSIE " + version + "; " + os + ")";

						client.Headers.Add (System.Net.HttpRequestHeader.UserAgent, string.Format ("FxGqlC/{3} ({0}; {1}; {2})", Environment.OSVersion.Platform, Environment.OSVersion.Version, Environment.OSVersion.VersionString, version));

						if (!nochecknewversion) {
							string type;
							if (versionType == 3)
								type = "";
							else
								type = "beta-";
							string urlRelease = string.Format ("https://sites.google.com/site/fxgqlc/home/downloads/release-{0}last.txt", type);
							byte[] data = client.DownloadData (urlRelease);
							string url;
							using (StreamReader r = new StreamReader(new MemoryStream(data))) {
								lastRelease = r.ReadLine ();
								url = r.ReadLine ();
							}

							//Console.WriteLine (lastRelease);
							//Console.WriteLine (version);
							//Console.WriteLine (url);
							if (CompareVersion(lastRelease, version) > 0 && url != null) {
								string fileName = null;
								try {
									fileName = System.IO.Path.GetTempFileName ();
									client.DownloadFile (url, fileName);

									string appDir = Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location);
									string newVersionDir = 
										Path.Combine (
											appDir,
											"NewVersion");
									if (Directory.Exists (newVersionDir))
										Directory.Delete (newVersionDir, true);
									Directory.CreateDirectory (newVersionDir);
									ExtractZipFile (fileName, null, newVersionDir);

									string oldVersionDir = 
										Path.Combine (
											appDir,
											"OldVersion");
									if (Directory.Exists (oldVersionDir))
										Directory.Delete (oldVersionDir, true);
									Directory.CreateDirectory (oldVersionDir);

									string[] files = Directory.GetFiles (newVersionDir);
									foreach (string file in files) {
										//Console.WriteLine (file);
										string fileName2 = Path.GetFileName (file);
										string appDirFile = Path.Combine (appDir, fileName2);
										//Console.WriteLine (appDirFile);
										//Console.WriteLine (Path.Combine (oldVersionDir, fileName2));
										//Console.WriteLine (appDirFile);
										if (File.Exists (appDirFile))
											File.Move (appDirFile, Path.Combine (oldVersionDir, fileName2));
										File.Move (file, appDirFile);
							
										try {
											if (Path.GetExtension (appDirFile).Equals (".exe", StringComparison.InvariantCultureIgnoreCase)) {
												Process ExeScript = new Process();
												ExeScript.StartInfo.FileName = "chmod";
												ExeScript.StartInfo.Arguments = "+x \"" + appDirFile + "\"";
												ExeScript.Start ();
											}
										} catch {
										}
									}
																		
									// Silent upgrade: No more display message to the user about the new version
									nochecknewversion = true;
								} catch (Exception) {
									//Console.WriteLine (x);
								} finally {
									try {
										if (fileName != null && File.Exists (fileName))
											File.Delete (fileName);
									} catch {
									}
								}

							} else {
								// only check version 1 time
								nochecknewversion = true;
							}
						}

						if (!notracking) {
							Random rnd = new Random ();

							long timestampFirstRun, timestampLastRun, timestampCurrentRun, numberOfRuns;

// Get the first run time
							timestampFirstRun = 0; //Settings.Default.FirstRun;
							timestampLastRun = 0; //Settings.Default.LastRun;
							timestampCurrentRun = 1000000000;
							numberOfRuns = 1; //Settings.Default.NumberOfRuns + 1;

// If we've never run before, we need to set the same values
							if (numberOfRuns == 1) {
								timestampFirstRun = timestampCurrentRun;
								timestampLastRun = timestampCurrentRun;
							}

// Some values we need
							string domainHash = ""; // This can be calcualted for your domain online
							string source = version;
							string medium = "FxGqlC";
							string sessionNumber = "1";
							string campaignNumber = "1";
							string screenRes = System.Console.WindowWidth + "x" + System.Console.WindowHeight;

							string stateName;
							switch (state) {
							case State.Start:
								stateName = "B";
								break;
							default:
							case State.Continue:
								stateName = "C";
								break;
							case State.Stop:
								stateName = "E";
								break;
							}
							string requestPath = "%2F" + stateName + "%2F" + version;
							string requestName = stateName + "%20" + version;

							string statsRequest = "http://www.google-analytics.com/__utm.gif" +
								"?utmwv=4.6.5" +
								"&utmn=" + rnd.Next (100000000, 999999999) +
								"&utmhn=" + Uri.EscapeDataString (Environment.UserName + '@' + GetFQDN ()) +
								"&utmcs=" + Uri.EscapeDataString (Console.OutputEncoding.WebName) +
								"&utmsr=" + screenRes +
								"&utmsc=-" +
								"&utmul=" + culture +
								"&utmje=-" +
								"&utmfl=-" +
								"&utmdt=" + requestName +
								"&utmhid=1943799692" +
								"&utmr=0" +
								"&utmp=" + requestPath +
								"&utmac=UA-2703249-8" + // Account number
								"&utmcc=" +
								"__utma%3D" + domainHash + "." + uniqueVisitorId + "." + timestampFirstRun + "." + timestampLastRun + "." + timestampCurrentRun + "." + numberOfRuns +
								"%3B%2B__utmz%3D" + domainHash + "." + timestampCurrentRun + "." + sessionNumber + "." + campaignNumber + ".utmcsr%3D" + source + "%7Cutmccn%3D(" + medium + ")%7Cutmcmd%3D" + medium + "%7Cutmcct%3D%2Fd31AaOM%3B";

							//Console.WriteLine (statsRequest);
							client.DownloadString (statsRequest);
							//Console.WriteLine ("OK");
							break;
						}
					}
				} catch { /*(Exception x)*/
					//Console.WriteLine (x);
					System.Threading.Thread.Sleep (5000);
				}

				if (System.Threading.Interlocked.Decrement (ref updatesBusy) == 0) {
					manualResetEvent.Set ();
				}
			}
		}

		static int CompareVersion (string lastRelease, string version)
		{
			string [] lastReleaseItems = lastRelease.Split('.');
			string [] versionItems = version.Split('.');
			
			int a, b, comp;
			if (int.TryParse(lastReleaseItems[0].Trim('v'), out a)
			    && int.TryParse(versionItems[0].Trim('v'), out b)) {
				comp = a.CompareTo(b);
				if (comp != 0)
					return comp;
			}
			
			if (int.TryParse(lastReleaseItems[1], out a)
			    && int.TryParse(versionItems[1], out b)) {
				comp = a.CompareTo(b);
				if (comp != 0)
					return comp;
			}
			
			if (lastReleaseItems[2].StartsWith("alpha"))
				a = 0;
			else if (lastReleaseItems[2].StartsWith("beta"))
				a = 1;
			else if (lastReleaseItems[2].StartsWith("rc"))
				a = 2;
			else
				a = 3;

			if (versionItems[2].StartsWith("alpha"))
				b = 0;
			else if (versionItems[2].StartsWith("beta"))
				b = 1;
			else if (versionItems[2].StartsWith("rc"))
				b = 2;
			else
				b = 3;
			
			comp = a.CompareTo (b);
			if (comp != 0)
				return comp;
			
			Match matchA, matchB;
			matchA = Regex.Match (lastReleaseItems[2], @"\d+");
			matchB = Regex.Match (versionItems[2], @"\d+");
			
			if (matchA.Success && matchB.Success
			    && int.TryParse (matchA.Value, out a)
			    && int.TryParse (matchB.Value, out b)) {
				comp = a.CompareTo(b);
				if (comp != 0)
					return comp;
			}
			
			return lastRelease.CompareTo(version);
		}

		public static string GetFQDN ()
		{
			string domainName = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties ().DomainName;
			string hostName = System.Net.Dns.GetHostName ();
			if (domainName == "(local)") domainName = "";
			string fqdn = "";
			if (!hostName.Contains (domainName) && domainName != "")
				fqdn = hostName + "." + domainName;
			else
				fqdn = hostName;

			return fqdn;
		}

		static void CheckToDisplayNewVersionMessage ()
		{
			if (nochecknewversion && notracking)
				return;

			if (!nochecknewversion && noautoupdate && lastRelease != null) {
				if (lastRelease.CompareTo (version) > 0) {
					Console.WriteLine ("A new version version of FxGqlC is available on https://sites.google.com/site/fxgqlc/home");
					Console.WriteLine ("Your version is {0} and the new version is {1}", version, lastRelease);
				}

				nochecknewversion = true;
			}
			CheckForUpdates (State.Continue);
		}

		static int GetUniqueId ()
		{
			var u = new System.Net.Sockets.UdpClient ("www.google-analytics.com", 1);		
			var localAddr = ((System.Net.IPEndPoint)u.Client.LocalEndPoint).Address;

			foreach (var nic in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()) {
				var ipProps = nic.GetIPProperties ();
				foreach (var address in ipProps.UnicastAddresses) {
					if (address.Address.Equals (localAddr)) {
						var mac = nic.GetPhysicalAddress ().GetAddressBytes ();
						ulong hash = 0;
						foreach (byte b in mac) {
							hash = (hash << 8) | b;
						}
						hash ^= 0x00000003; // 'version'
						return (int)((hash % 899999999) + 1000000000);
					}
				}
			}

			return new Random ((int)(DateTime.Now.Ticks % ((long)int.MaxValue + 1))).Next (100000000, 999999999); // Random
		}

		static void ReportException (string context, Exception x)
		{
			if (x is ParserException) {
				if ((reportError & ReportError.ParsingExceptions) != 0)
					ReportExceptionStep2 (context, x);
			} else if (x is InvalidOperationException) {
				if ((reportError & ReportError.ExecutionExceptions) != 0)
					ReportExceptionStep2 (context, x);
			} else {
				if ((reportError & ReportError.ApplicationExceptions) != 0)
					ReportExceptionStep2 (context, x);
			}
		}

		static void ReportExceptionStep2 (string context, Exception x)
		{
//			System.Net.Mail.MailMessage message = new System.Net.Mail.MailMessage ();
//			message.To.Add (new System.Net.Mail.MailAddress ("wimobiwan+fxgqlc@gmail.com"));
//			try {
//				message.From = new System.Net.Mail.MailAddress (fromAddress);
//			} catch {
//			}
//			message.Subject = "ReportException - " + version + " - " + x.Message;
//			System.Text.StringBuilder sb = new System.Text.StringBuilder ();
//			sb.Append ("UTC DateTime:");
//			sb.Append (DateTime.UtcNow);
//			sb.Append ("Context:");
//			sb.Append (context);
//			sb.Append ("Exception:");
//			sb.Append (x.ToString ());
//			S
//			message.Body = sb.ToString ();
//
//			string fromAddress;
//			if ((reportError & ReportError.NoConfirm) != 0) {
//				fromAddress = null;
//			} else {
//				Console.WriteLine ("Do you want this exception to be sent to the FxGqlC developer?");
//				Console.WriteLine ("(Look for '!SET ERRORREPORT' in the manual on the FxGqlC website for more ");
//				Console.WriteLine (" information on enabling/disabling the error reporting)");
//				Console.WriteLine ("Enter your e-mail address when you want this error report to be sent, or ");
//				Console.WriteLine (" just press enter to ignore the exception.");
//				Console.Write ("Your E-mail address [or empty]: ");
//				fromAddress = Console.ReadLine ();
//			}
//			if (fromAddress == null || fromAddress != "")
//				ReportExceptionStep3 (context, x, fromAddress);
//		}
//
//		static void ReportExceptionStep3 (string context, Exception x, string fromAddress)
//		{
		}

		public static void ExtractZipFile (string archiveFilenameIn, string password, string outFolder)
		{
			ICSharpCode.SharpZipLib.Zip.ZipFile zf = null;
			try {
				FileStream fs = File.OpenRead (archiveFilenameIn);
				zf = new ICSharpCode.SharpZipLib.Zip.ZipFile (fs);
				if (!String.IsNullOrEmpty (password)) {
					zf.Password = password;		// AES encrypted entries are handled automatically
				}
				foreach (ICSharpCode.SharpZipLib.Zip.ZipEntry zipEntry in zf) {
					if (!zipEntry.IsFile) {
						continue;			// Ignore directories
					}
					String entryFileName = zipEntry.Name;
					// to remove the folder from the entry:- entryFileName = Path.GetFileName(entryFileName);
					// Optionally match entrynames against a selection list here to skip as desired.
					// The unpacked length is available in the zipEntry.Size property.

					byte[] buffer = new byte[4096];		// 4K is optimum
					Stream zipStream = zf.GetInputStream (zipEntry);

					// Manipulate the output filename here as desired.
					String fullZipToPath = Path.Combine (outFolder, entryFileName);
					string directoryName = Path.GetDirectoryName (fullZipToPath);
					if (directoryName.Length > 0)
						Directory.CreateDirectory (directoryName);

					// Unzip file in buffered chunks. This is just as fast as unpacking to a buffer the full size
					// of the file, but does not waste memory.
					// The "using" will close the stream even if an exception occurs.
					using (FileStream streamWriter = File.Create(fullZipToPath)) {
						ICSharpCode.SharpZipLib.Core.StreamUtils.Copy (zipStream, streamWriter, buffer);
					}
				}
			} finally {
				if (zf != null) {
					zf.IsStreamOwner = true; // Makes close also shut the underlying stream
					zf.Close (); // Ensure we release resources
				}
			}
		}
	}
}
