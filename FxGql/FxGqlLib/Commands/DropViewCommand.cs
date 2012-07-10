using System;
using System.IO;

namespace FxGqlLib
{
	public class DropViewCommand : IGqlCommand
	{
		readonly string name;

		public DropViewCommand (string name)
		{
			this.name = name;
		}

        #region IGqlCommand implementation
		public void Execute (TextWriter outputStream, TextWriter logStream, GqlEngineState gqlEngineState)
		{
			if (!gqlEngineState.Views.Remove (name))
				throw new InvalidOperationException (string.Format ("View {0} doesn't exist.", name));
		}
        #endregion
	}
}

