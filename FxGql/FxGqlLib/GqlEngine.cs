using System;
using System.IO;
using System.Collections.Generic;

namespace FxGqlLib
{
	public class GqlEngine
	{
		TextWriter outputStream;
		
		public TextWriter OutputStream
		{
			get { return outputStream; }
			set { outputStream = value; }
		}
		
		public GqlEngine ()
		{
		}
	
		public void Execute(string commandsText)
		{
			GqlParser parser = new GqlParser(commandsText);
			IList<IGqlCommand> commands = parser.Parse();
			
			foreach (IGqlCommand command in commands)
			{
				command.Execute(outputStream);
			}
		}
	}
}

