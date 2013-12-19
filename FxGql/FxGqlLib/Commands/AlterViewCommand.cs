using System;
using System.IO;

namespace FxGqlLib
{
	public class AlterViewCommand : IGqlCommand
	{
		readonly string name;
		readonly ViewDefinition viewDefinition;

		public AlterViewCommand (string name, ViewDefinition viewDefinition)
		{
			this.name = name;
			this.viewDefinition = viewDefinition;
		}

		#region IGqlCommand implementation

		public void Execute (TextWriter outputStream, TextWriter logStream, GqlEngineState gqlEngineState)
		{
			if (!gqlEngineState.Views.ContainsKey (name))
				throw new InvalidOperationException (string.Format ("View {0} doesn't exist.", name));
			gqlEngineState.Views [name] = viewDefinition;
		}

		#endregion

	}
}

