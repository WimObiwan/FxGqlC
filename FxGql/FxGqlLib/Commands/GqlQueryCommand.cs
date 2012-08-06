using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FxGqlLib
{
	public class GqlQueryCommand : IGqlCommand
	{
		readonly IProvider gqlQuery;
		
		public GqlQueryCommand (IProvider gqlQuery)
		{
			this.gqlQuery = gqlQuery;
		}

		#region IGqlCommand implementation
		public void Execute (TextWriter outputStream, TextWriter logStream, GqlEngineState gqlEngineState)
		{
			GqlQueryState gqlQueryState = new GqlQueryState (gqlEngineState);

			IntoProvider.DumpProviderToStream (gqlQuery, outputStream, gqlQueryState, "\t", gqlEngineState.Heading,
			                                   gqlEngineState.AutoSize);
		}
		#endregion
	}
}

