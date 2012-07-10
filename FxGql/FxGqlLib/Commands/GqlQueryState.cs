using System;
using System.Collections.Generic;

namespace FxGqlLib
{
	public class GqlQueryState
	{
		public GqlQueryState (GqlEngineExecutionState currentExecutionState, Dictionary<string, Variable> variables)
		{
			this.CurrentExecutionState = currentExecutionState;
			this.Variables = variables;
			this.StateBin = new StateBin ();
		}
		
		public ProviderRecord Record { get; set; }

		public long TotalLineNumber { get; set; }

		public bool UseOriginalColumns { get; set; }
		
		public string CurrentDirectory { get; set; }
		
		public GqlEngineExecutionState CurrentExecutionState { get; private set; }

		public Dictionary<string, Variable> Variables { get; private set; }

		public StateBin StateBin { get; private set; }
	}
}

