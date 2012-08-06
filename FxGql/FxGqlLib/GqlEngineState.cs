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
			Views = new Dictionary<string, ViewDefinition> (StringComparer.InvariantCultureIgnoreCase);
			AutoSize = 0;
			ColumnDelimiter = "\t";
		}
		
		public GqlEngineExecutionState ExecutionState { get; private set; }

		string currentDirectory;
		public delegate void CurrentDirectoryChangedHandler ();
		public event CurrentDirectoryChangedHandler CurrentDirectoryChanged;
		public string CurrentDirectory { 
			get { 
				return currentDirectory;
			}
			set {
				if (currentDirectory != value) {
					currentDirectory = value;
					if (CurrentDirectoryChanged != null)
						CurrentDirectoryChanged ();
				}
			}
		}

		public string TempDirectory { get; set; }

		public enum HeadingEnum
		{
			Off,
			On,
			OnWithRule
		}
		public HeadingEnum Heading { get; set; }

		public Dictionary<string, Variable> Variables { get; private set; }
		public Dictionary<string, ViewDefinition> Views { get; private set; }
		public int AutoSize { get; set; }
		public string ColumnDelimiter { get; set; }
	}
}

