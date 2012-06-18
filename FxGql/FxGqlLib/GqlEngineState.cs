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

		public enum HeadingsEnum { Off, On, OnWithRule }
		public HeadingsEnum Headings { get; set; }
	}
}

