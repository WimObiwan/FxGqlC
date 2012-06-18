using System;
using System.IO;
using System.Collections.Generic;


namespace FxGqlLib
{
	public class DeclareCommand : IGqlCommand
	{
		IList<Tuple<string, Type>> variables;

		public DeclareCommand (IList<Tuple<string, Type>> variables)
		{
			this.variables = variables;
		}

		#region IGqlCommand implementation
		public void Execute (TextWriter outputStream, TextWriter logStream, GqlEngineState gqlEngineState)
		{
		}
		#endregion
	}
}

