using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace FxGqlLib
{
	public class FileSubqueryProvider : MultiFileProviderBase
	{
		readonly IProvider fileSubqueryProvider;

		public FileSubqueryProvider (IProvider fileSubqueryProvider)
		{
			this.fileSubqueryProvider = fileSubqueryProvider;
		}

		public override void OnInitialize (GqlQueryState gqlQueryState, out string[] files, out long skip)
		{
			fileSubqueryProvider.Initialize (gqlQueryState);
			try {
				List<string> fileList = new List<string> ();
				while (fileSubqueryProvider.GetNextRecord()) {
					fileList.Add (fileSubqueryProvider.Record.Columns [0].ToDataString ());
				}
				files = fileList.ToArray ();
			} finally {
				fileSubqueryProvider.Uninitialize ();
			}

			skip = 0;
		}

		public override void OnUninitialize ()
		{
		}
	}
}

