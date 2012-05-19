using System;

namespace FxGqlLib
{
	public class GqlQueryState
	{
		public GqlQueryState (GqlEngineExecutionState currentExecutionState)
		{
			this.CurrentExecutionState = currentExecutionState;
		}
		
		public ProviderRecord Record { get; set; }

		public long TotalLineNumber { get; set; }

		public bool UseOriginalColumns { get; set; }
		
		public string CurrentDirectory { get; set; }
		
		public GqlEngineExecutionState CurrentExecutionState { get; private set; }
	}
}

