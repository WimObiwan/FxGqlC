using System;
using System.IO;

namespace FxGqlLib
{
	public class CreateViewCommand : IGqlCommand
	{
		readonly string name;
		readonly ViewDefinition viewDefinition;

		public CreateViewCommand (string name, ViewDefinition viewDefinition)
		{
			this.name = name;
			this.viewDefinition = viewDefinition;
		}

		#region IGqlCommand implementation

		public void Execute (TextWriter outputStream, TextWriter logStream, GqlEngineState gqlEngineState)
		{
			gqlEngineState.Views.Add (name, viewDefinition);
		}

		#endregion

	}
}

