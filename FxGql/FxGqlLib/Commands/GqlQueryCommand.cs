using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
			FormatColumnListFunction formatColumnListFunction = new  FormatColumnListFunction ("\t");
			using (SelectProvider provider = new SelectProvider (
					new List<IExpression> () { formatColumnListFunction },
					gqlQuery)) {
				
				try {
					GqlQueryState gqlQueryState = new GqlQueryState (gqlEngineState.ExecutionState);
					gqlQueryState.CurrentDirectory = gqlEngineState.CurrentDirectory;
					provider.Initialize (gqlQueryState);

					/*
					{
						string[] columnTitles = gqlQuery.GetColumnTitles ();
						string text = formatColumnListFunction.Evaluate (columnTitles);
						outputStream.WriteLine (text);
						if (logStream != null)
							logStream.WriteLine (text);

						text = formatColumnListFunction.Evaluate (columnTitles.Select(p => new string('=', p.Length)).ToArray());
						outputStream.WriteLine (text);
						if (logStream != null)
							logStream.WriteLine (text);
					}
					*/

					while (provider.GetNextRecord()) {
						string text = provider.Record.Columns [0].ToString ();
						outputStream.WriteLine (text);
						if (logStream != null)
							logStream.WriteLine (text);
					}
					
				} finally {
					provider.Uninitialize ();
				}
			}
		}
		#endregion
	}
}

