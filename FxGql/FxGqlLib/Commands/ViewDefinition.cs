using System;
using System.Collections.Generic;

namespace FxGqlLib
{
	public class ViewDefinition
	{
		public IProvider Provider { get; private set; }
		public IList<Tuple<string, Type>> Parameters { get; private set; }

		public ViewDefinition (IProvider provider, IList<Tuple<string, Type>> parameters)
		{
			Provider = provider;
			Parameters = parameters;
		}
	}
}

