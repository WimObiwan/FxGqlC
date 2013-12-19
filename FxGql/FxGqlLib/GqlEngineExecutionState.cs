using System;

namespace FxGqlLib
{
	public class GqlEngineExecutionState
	{
		public enum InterruptStates
		{
			Continue,
			Interrupted,
		}

		public GqlEngineExecutionState ()
		{
			InterruptState = InterruptStates.Continue;
		}

		public InterruptStates InterruptState { get; set; }
	}
}

