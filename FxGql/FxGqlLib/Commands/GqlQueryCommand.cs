using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;

namespace FxGqlLib
{
	public class GqlQueryCommand : IGqlCommand
	{
		readonly IProvider gqlQuery;
		readonly CultureInfo cultureInfo;
		
		public GqlQueryCommand (IProvider gqlQuery, CultureInfo cultureInfo)
		{
			this.gqlQuery = gqlQuery;
			this.cultureInfo = cultureInfo;
		}

		#region IGqlCommand implementation
		public void Execute (TextWriter outputStream, TextWriter logStream, GqlEngineState gqlEngineState)
		{
			GqlQueryState gqlQueryState = new GqlQueryState (gqlEngineState);

			IntoProvider.DumpProviderToStream (gqlQuery, outputStream, gqlQueryState, gqlEngineState.ColumnDelimiter, gqlEngineState.Heading,
			                                   gqlEngineState.AutoSize, FileOptionsIntoClause.FormatEnum.DontCare, cultureInfo);
		}
		#endregion
	}
}

