using System;
using System.Collections.Generic;

namespace FxGqlLib
{
	public class GqlQueryState
	{
//		public GqlQueryState (GqlEngineExecutionState currentExecutionState, Dictionary<string, Variable> variables)
//		{
//			this.CurrentExecutionState = currentExecutionState;
//			this.Variables = variables;
//			this.StateBin = new StateBin ();
//		}
//		

		public GqlQueryState ()
		{
			this.StateBin = new StateBin ();
		}

		public GqlQueryState (GqlEngineState other)
			: this()
		{
			this.CurrentDirectory = other.CurrentDirectory;
			this.TempDirectory = other.TempDirectory;
			this.CurrentExecutionState = other.ExecutionState;
			this.Variables = other.Variables;
		}

		public GqlQueryState (GqlQueryState other)
			: this()
		{
			this.CurrentDirectory = other.CurrentDirectory;
			this.TempDirectory = other.TempDirectory;
			this.CurrentExecutionState = other.CurrentExecutionState;
			this.Variables = other.Variables;
		}

		public ProviderRecord Record { get; set; }

		public long TotalLineNumber { get; set; }

		public bool UseOriginalColumns { get; set; }
		
		public string CurrentDirectory { get; set; }

		public string TempDirectory { get; set; }

		public GqlEngineExecutionState CurrentExecutionState { get; private set; }

		public Dictionary<string, Variable> Variables { get; private set; }

		public StateBin StateBin { get; private set; }
	}
}

