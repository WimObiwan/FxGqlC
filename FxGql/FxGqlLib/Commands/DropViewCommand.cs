using System;
using System.IO;

namespace FxGqlLib
{
	public class DropViewCommand : IGqlCommand
	{
		string name;

		public DropViewCommand (string name)
		{
			this.name = name;
		}

        #region IGqlCommand implementation
		public void Execute (TextWriter outputStream, TextWriter logStream, GqlEngineState gqlEngineState)
		{
			gqlEngineState.Views.Remove (name);
		}
        #endregion
	}
}

