using System;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;

namespace FxGqlLib
{
	public class DataComparer : IComparer<IData>
	{
		public bool CaseInsensitive { get; private set; }
		public StringComparer StringComparer { get; private set; }
		public StringComparison StringComparison { get; private set; }
		public CultureInfo CultureInfo { get; private set; }

		public DataComparer (CultureInfo cultureInfo, bool caseInsensitive)
		{
			this.CultureInfo = cultureInfo;
			this.CaseInsensitive = caseInsensitive;
			this.StringComparer = StringComparer.Create (cultureInfo, caseInsensitive);
			if (cultureInfo == CultureInfo.InvariantCulture) {
				if (caseInsensitive)
					StringComparison = StringComparison.InvariantCultureIgnoreCase;
				else
					StringComparison = StringComparison.InvariantCulture;
			} else {
				// TODO: This is not correct...  Eliminate use of StringComparison!
				if (caseInsensitive)
					StringComparison = StringComparison.CurrentCultureIgnoreCase;
				else
					StringComparison = StringComparison.CurrentCulture;
			}
		}

		#region IComparer implementation
		public int Compare (IData x, IData y)
		{
			if (x is DataString || y is DataString)
				return StringComparer.Compare (x.ToDataString (CultureInfo).Value, y.ToDataString (CultureInfo).Value);
			else
				return x.CompareTo (y);
		}
		#endregion		
	}
}

