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
			: this ()
		{
			this.CurrentDirectory = other.CurrentDirectory;
			this.TempDirectory = other.TempDirectory;
			this.CurrentExecutionState = other.ExecutionState;
			this.Variables = other.Variables;
			this.Warnings = other.Warnings;
		}

		public GqlQueryState (GqlQueryState other)
			: this (other, false)
		{
		}

		public GqlQueryState (GqlQueryState other, bool newVariableScope)
			: this ()
		{
			this.CurrentDirectory = other.CurrentDirectory;
			this.TempDirectory = other.TempDirectory;
			this.CurrentExecutionState = other.CurrentExecutionState;
			if (newVariableScope)
				this.Variables = new Dictionary<string, Variable> (other.Variables);
			else
				this.Variables = other.Variables;
			this.Warnings = other.Warnings;
		}

		public ProviderRecord Record { get; set; }

		public long TotalLineNumber { get; set; }

		public bool UseOriginalColumns { get; set; }

		public string CurrentDirectory { get; set; }

		public string TempDirectory { get; set; }

		public GqlEngineExecutionState CurrentExecutionState { get; private set; }

		public Dictionary<string, Variable> Variables { get; private set; }

		public StateBin StateBin { get; private set; }

		public bool SkipLine { get; set; }

		public IList<Exception> Warnings { get; private set; }
	}
}

