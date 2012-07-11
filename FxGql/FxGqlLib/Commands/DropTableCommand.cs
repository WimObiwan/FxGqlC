using System;
using System.Diagnostics;
using System.Linq;
using Antlr.Runtime;
using Antlr.Runtime.Tree;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace FxGqlLib
{
	class DropTableCommand : IGqlCommand
	{
		FileOptions fileOptions;

		public DropTableCommand (FileOptions fileOptions)
		{
			this.fileOptions = fileOptions;
		}

		public void Execute (TextWriter outputStream, TextWriter logStream, GqlEngineState gqlEngineState)
		{
			string fileName = this.fileOptions.FileName.EvaluateAsString (null);
			if (fileName.StartsWith ("#")) {
				fileName = Path.Combine (gqlEngineState.TempDirectory, fileName);
			} else {
				fileName = Path.Combine (gqlEngineState.CurrentDirectory, fileName);
			}
			File.Delete (fileName);
		}
	}

    
	
}

