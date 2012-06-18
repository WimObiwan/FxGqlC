using System;

namespace FxGqlLib
{
	public class GqlEngineState
	{
		public GqlEngineState (GqlEngineExecutionState executionState)
		{
			ExecutionState = executionState;
		}
		
		public GqlEngineExecutionState ExecutionState { get; private set; }

		public string CurrentDirectory { get; set; }

		public enum HeadingEnum { Off, On, OnWithRule }
		public HeadingEnum Heading { get; set; }
	}
}

