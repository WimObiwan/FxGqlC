using System;

namespace FxGqlLib
{
	public static class DataUtilities
	{
		static IComparer GetComparer (StringComparer stringComparer)
		{
			return new DataStringComparer (stringComparer);
		}
	}
}

