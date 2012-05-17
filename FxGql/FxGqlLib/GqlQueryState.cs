using System;

namespace FxGqlLib
{
	public class GqlQueryState
	{
		public GqlQueryState ()
		{
		}
		
		public ProviderRecord Record { get; set; }

		public long TotalLineNumber { get; set; }

		public bool UseOriginalColumns { get; set; }
		
		public string CurrentDirectory { get; set; }
	}
}

