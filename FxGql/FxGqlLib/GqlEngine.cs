using System;
using System.IO;
using System.Collections.Generic;

namespace FxGqlLib
{
    public class GqlEngine
    {
        TextWriter outputStream;
        TextWriter logStream;
        GqlEngineExecutionState gqlEngineExecutionState = new GqlEngineExecutionState ();

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
        }
	
        public void Execute (string commandsText)
        {
            gqlEngineExecutionState.InterruptState = GqlEngineExecutionState.InterruptStates.Continue;
			
            if (logStream != null) {
                logStream.WriteLine ("===========================================================================");				
                logStream.WriteLine ("Gql> {0}", commandsText);
            }
            GqlParser parser = new GqlParser (commandsText);
            IList<IGqlCommand> commands = parser.Parse ();
			
            foreach (IGqlCommand command in commands) {
                command.Execute (outputStream, logStream, GqlEngineState);
            }
        }

        public void Reset ()
        {
            GqlEngineState.Variables.Clear ();
        }		

        public void Interrupt ()
        {
            gqlEngineExecutionState.InterruptState = GqlEngineExecutionState.InterruptStates.Interrupted;
        }
    }
}

