using System;

namespace FxGqlLib
{
	public class GqlEngineState
	{
		public GqlEngineState (GqlEngineExecutionState executionState)
		{
			ExecutionState = executionState;
		}
		
		public string CurrentDirectory { get; set; }
		public GqlEngineExecutionState ExecutionState { get; private set; }
	}
}

