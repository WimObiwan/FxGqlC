using System;
using System.Collections.Generic;

namespace FxGqlLib
{
	public class GqlEngineState
	{
		public GqlEngineState (GqlEngineExecutionState executionState)
		{
			ExecutionState = executionState;
			Variables = new Dictionary<string, Variable> (StringComparer.InvariantCultureIgnoreCase);
			Views = new Dictionary<string, IProvider> (StringComparer.InvariantCultureIgnoreCase);
		}
		
		public GqlEngineExecutionState ExecutionState { get; private set; }

		public string CurrentDirectory { get; set; }

		public enum HeadingEnum
		{
			Off,
			On,
			OnWithRule
		}
		public HeadingEnum Heading { get; set; }

		public Dictionary<string, Variable> Variables { get; private set; }
		public Dictionary<string, IProvider> Views { get; private set; }
	}
}

