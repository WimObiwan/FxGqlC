using System;
using System.IO;
using System.Collections.Generic;


namespace FxGqlLib
{
	public class DeclareCommand : IGqlCommand
	{
		readonly IList<Tuple<string, Type>> variables;

		public DeclareCommand (IList<Tuple<string, Type>> variables)
		{
			this.variables = variables;
		}

		#region IGqlCommand implementation
		public void Execute (TextWriter outputStream, TextWriter logStream, GqlEngineState gqlEngineState)
		{
			foreach (Tuple<string, Type> variableDeclaration in variables) {
				Variable variable = new Variable (variableDeclaration.Item1, variableDeclaration.Item2);
				if (gqlEngineState.Variables.ContainsKey (variable.Name))
					throw new InvalidOperationException (string.Format ("The variable '{0}' is already declared.", variable.Name));
				gqlEngineState.Variables.Add (variable.Name, variable);
			}

		}
		#endregion
	}
}

