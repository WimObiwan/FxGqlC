using System.IO;
using System;
using System.Collections.Generic;

namespace FxGqlLib
{
	public class GqlQueryCommand : IGqlCommand
	{
		IProvider gqlQuery;
		
		public GqlQueryCommand (IProvider gqlQuery)
		{
			this.gqlQuery = gqlQuery;
		}

		#region IGqlCommand implementation
		public void Execute (TextWriter outputStream, TextWriter logStream, GqlEngineState gqlEngineState)
		{
			using (SelectProvider provider = new SelectProvider (
					new List<IExpression> () { new FormatColumnListFunction ("\t") },
					gqlQuery)) {
				
				GqlQueryState gqlQueryState = new GqlQueryState();
				gqlQueryState.CurrentDirectory = gqlEngineState.CurrentDirectory;
				provider.Initialize (gqlQueryState);
					
				while (provider.GetNextRecord()) {
					string text = provider.Record.Columns [0].ToString ();
					outputStream.WriteLine (text);
					if (logStream != null)
						logStream.WriteLine (text);
				}
				
				provider.Uninitialize ();
			}
		}
		#endregion
	}
}

