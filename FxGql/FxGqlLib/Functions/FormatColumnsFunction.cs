using System;
using System.Collections.Generic;

namespace FxGqlLib
{
	public interface FormatColumnsFunction
	{
		DataString Evaluate (IEnumerable<string> columns);
	}
}

