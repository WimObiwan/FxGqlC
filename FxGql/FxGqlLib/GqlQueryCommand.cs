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
		public void Execute (TextWriter outputStream)
		{
			using (SelectProvider provider = new SelectProvider (
					new List<IExpression> () { new FormatColumnListFunction ("\t") },
					gqlQuery)) {

				provider.Initialize ();
					
				while (provider.GetNextRecord()) {
					outputStream.WriteLine (provider.Record.Columns [0].ToString ());
				}
				
				provider.Uninitialize ();
			}
		}
		#endregion
	}
}

