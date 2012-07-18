using System;
using FxGqlLib;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

namespace FxGqlC
{
	class MainClass
	{
		static GqlEngine gqlEngine;
		static string version;
		static string lastRelease;
		static bool nochecknewversion = false;
		static bool notracking = false;
		static DateTime lastCheck = DateTime.MinValue;
		static int uniqueVisitorId = new Random ((int)(DateTime.Now.Ticks % ((long)int.MaxValue + 1))).Next (100000000, 999999999); // Random

		static string GetVersion ()
		{
			var info = System.Diagnostics.FileVersionInfo.GetVersionInfo (Assembly.GetExecutingAssembly ().Location);
			string type;
			switch (info.FileBuildPart) {
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

			return version;
		}

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
			string logFile = null;
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
						errors.Add ("Please specify a GQL file after '-file'");
				} else if (string.Equals (args [i], "-logfile", StringComparison.InvariantCultureIgnoreCase)) {
					i++;
					if (i < args.Length)
						logFile = args [i];
					else
						errors.Add ("Please specify an output file after '-logfile'");
				} else if (string.Equals (args [i], "-autoexec", StringComparison.InvariantCultureIgnoreCase)) {
					i++;
					if (i < args.Length)
						autoexec = args [i];
					else
						errors.Add ("Please specify a GQL file after '-autoexec'");
				} else if (string.Equals (args [i], "-nochecknewversion", StringComparison.InvariantCultureIgnoreCase)) {
					nochecknewversion = true;
					notracking = true;
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
				if (logFile != null)
					gqlEngine.LogStream = new StreamWriter (logFile);

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

		public static void RunPrompt ()
		{
			Mono.Terminal.LineEditor lineEditor = new Mono.Terminal.LineEditor ("editor");
			while (true) {
				string command = lineEditor.Edit ("FxGqlC> ", "");
				//Console.Write ("FxGqlC> ");
				//string command = Console.ReadLine ();
				if (command.Trim ().Equals ("exit", StringComparison.InvariantCultureIgnoreCase))
					break; 
				ExecuteCommand (command);
				CheckToDisplayNewVersionMessage ();
			}
		}

		public static void ExecuteCommand (string command)
		{
			if (command.TrimStart ().StartsWith ("!")) {
				ExecuteClientCommand (command);
			} else {
				ExecuteServerCommand (command);
			}
		}

		static void ExecuteClientCommand (string command)
		{
			command = command.TrimStart ().TrimStart ('!');
			string[] commandComponents = command.Split (new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);

			if (commandComponents.Length < 1) {
				Console.WriteLine ("Invalid client command syntax");
			} else {
				switch (commandComponents [0].ToUpperInvariant ()) {
				case "SET":
					ExecuteClientCommandSet (commandComponents);
					break;
				default:
					Console.WriteLine ("Unknown client command '{0}'", commandComponents [0]);
					break;
				}
			}
		}

		public static void ExecuteServerCommand (string command)
		{
#if DEBUG
			gqlEngine.Execute (command);
#else
			try {
				gqlEngine.Execute (command);
			} catch (FxGqlLib.ParserException x) {
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
							Console.WriteLine ("     {0}^", new string (' ', Math.Max (0, x.Pos - 1)));
							if (gqlEngine.LogStream != null) 
								gqlEngine.LogStream.WriteLine ("     {0}^", new string (' ', Math.Max (0, x.Pos - 1)));
						}
					}
				}
			} catch (Exception x) {

				Console.WriteLine (x.Message);
				if (gqlEngine.LogStream != null) 
					gqlEngine.LogStream.WriteLine (x.ToString ());
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

		static void ExecuteClientCommandSet (string[] commandComponents)
		{
			if (commandComponents.Length < 3) {
				Console.WriteLine ("Invalid number of components in client command 'SET'");
			} else {
				string key = commandComponents [1];
				string value = commandComponents [2];
				switch (key.ToUpperInvariant ()) {
				case "HEADING":
					GqlEngineState.HeadingEnum heading;
					if (Enum.TryParse<GqlEngineState.HeadingEnum> (value, true, out heading)) 
						gqlEngine.GqlEngineState.Heading = heading;
					else
						Console.WriteLine ("Unknown SET HEADING value '{0}'", value);

					break;
				default:
					Console.WriteLine ("Unknown SET command '{0}'", key);
					break;
				}
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
			DateTime now = DateTime.Now;
			if (lastCheck == DateTime.MinValue || lastCheck + new TimeSpan (0, 15, 0) < now) {
				lastCheck = now;
				System.Threading.ThreadPool.QueueUserWorkItem (new System.Threading.WaitCallback (delegate(object state2) {
					CheckForUpdatesAsync (state);
				}
				)
				);
			}
		}

		static void CheckForUpdatesAsync (State state)
		{
			if (nochecknewversion && notracking)
				return;
			try {
				System.Net.ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback (delegate(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors) {
					return true;
				}
				);

				using (var client = new System.Net.WebClient ()) {

					string culture = System.Threading.Thread.CurrentThread.CurrentCulture.Name;
					//Mozilla/5.0 (Windows; U; Windows NT 6.1
					string os;
					os = System.Text.RegularExpressions.Regex.Replace (Environment.OSVersion.VersionString, @"^.*(Windows NT \d+\.\d+).*$", "$1");
					//client.Headers.Add (System.Net.HttpRequestHeader.UserAgent, "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 7.1; Trident/5.0)");
					client.Headers.Add (System.Net.HttpRequestHeader.UserAgent, "Mozilla/5.0 (compatible; MSIE 9.0; " + os + "; Trident/5.0)");
					//client.Headers ["user-agent"] = "Mozilla/5.0 (compatible; MSIE " + version + "; " + os + ")";

					if (!nochecknewversion) {
						byte[] data = client.DownloadData ("https://sites.google.com/site/fxgqlc/home/downloads/release-last.txt");
						using (StreamReader r = new StreamReader(new MemoryStream(data))) {
							lastRelease = r.ReadLine ();
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
							stateName = "AppStartup";
							break;
						default:
						case State.Continue:
							stateName = "AppContinue";
							break;
						case State.Stop:
							stateName = "AppStop";
							break;
						}
						string requestPath = "%2F" + stateName + "%2FRELEASE%2F" + version;
						string requestName = stateName + "%20v" + version;

						string statsRequest = "http://www.google-analytics.com/__utm.gif" +
							"?utmwv=4.6.5" +
							"&utmn=" + rnd.Next (100000000, 999999999) +
							"&utmhn=" + Uri.EscapeDataString ("sites.google.com/site/fxgqlc") +
							"&utmcs=-" +
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

						client.DownloadString (statsRequest);
					}
				}

			} catch {
			}
		}

		static void CheckToDisplayNewVersionMessage ()
		{
			if (nochecknewversion && notracking)
				return;

			if (!nochecknewversion && lastRelease != null) {
#if DEBUG
				if (true) {
#else
				if (lastRelease.CompareTo (version) > 0) {
#endif
					Console.WriteLine ("A new version version of FxGqlC is available on https://sites.google.com/site/fxgqlc/home");
					Console.WriteLine ("Your version is {0} and the new version is {1}", version, lastRelease);
				}

				nochecknewversion = true;
			}
			
			CheckForUpdates (State.Continue);
		}
	}
}
