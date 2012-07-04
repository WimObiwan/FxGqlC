using System;
using System.IO;

namespace FxGqlLib
{
	public class CreateViewCommand : IGqlCommand
	{
		string name;
		IProvider provider;

		public CreateViewCommand (string name, IProvider provider)
		{
			this.name = name;
			this.provider = provider;
		}

        #region IGqlCommand implementation
		public void Execute (TextWriter outputStream, TextWriter logStream, GqlEngineState gqlEngineState)
		{
			gqlEngineState.Views.Add (name, provider);
		}
        #endregion
	}
}

