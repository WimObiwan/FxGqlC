using System;
using FxGqlLib;

namespace FxGqlC
{
	class MainClass
	{
		static GqlEngine gqlEngine = new GqlEngine ();
		
		public static void Main (string[] args)
		{
			Console.WriteLine ("FxGqlC - Fox Innovations Grep Query Language for Console");
			Console.WriteLine ("(c) Copyright 2006-2012 Fox Innovations");

			RJH.CommandLineHelper.Parser parser = new RJH.CommandLineHelper.Parser (Environment.CommandLine);
			parser.AddSwitch (new string[] {"help", "h"}, "Show help");
			parser.AddSwitch (new string[] {"prompt", "p"}, "Run in prompt mode");
			parser.AddSwitch (new string[] {"command", "c"}, "Run a single command (usefull when running FxGqlC in scripts)");
			parser.AddSwitch (new string[] {"file", "f"}, "Run the commands from a file");
			
			parser.Parse ();
			
			if (parser ["help"] != null) {
				ShowHelp ();
			}
			if (parser ["prompt"] != null) {
				RunPrompt ();
			} else if (parser ["command"] != null) {
				ExecuteCommand (parser ["command"] as string);
			} else if (parser ["file"] != null) {
				ExecuteFile (parser ["file"] as string);
			} else {
				ShowHelp ();
			}
		}

		public static void ShowHelp ()
		{
			Console.WriteLine ("FxGqlC.exe");
			Console.WriteLine ("  -help or -h");
			Console.WriteLine ("    Show this help");
			Console.WriteLine ("  -prompt or -p");
			Console.WriteLine ("    Run in prompt mode");
			Console.WriteLine ("  -command or -c <command>");
			Console.WriteLine ("    Run a single command (usefull when running FxGqlC in scripts)");
			Console.WriteLine ("  -file or -f <file>");
			Console.WriteLine ("    Run the commands from a file");
		}

		public static void RunPrompt ()
		{
			while (true) {
				Console.Write ("FxGqlC> ");
				string command = Console.ReadLine ();
				if (command.Equals ("exit", StringComparison.InvariantCultureIgnoreCase))
					break; 
				try {
					ExecuteCommand (command);
				} catch (Exception x) {
					Console.WriteLine (x.ToString ());
				}
			}
		}

		public static void ExecuteCommand (string command)
		{
			gqlEngine.OutputStream = Console.Out;
			gqlEngine.Execute (command);
		}

		public static void ExecuteFile (string file)
		{
			using (System.IO.StreamReader reader = new System.IO.StreamReader(file)) {
				string command = reader.ReadToEnd ();
				ExecuteCommand (command);
			}
		}
	}
}
