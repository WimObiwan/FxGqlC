using System;

namespace FxGqlLib
{
	public static class StringExtensions
	{
		public static DataString CommonPrefix (this DataString str1, DataString str2)
		{
			int len1 = str1.Value.Length;
			int len2 = str2.Value.Length;
			int len = Math.Min (len1, len2);

			int common = 0;
			while (common < len && str1.Value [common] == str2.Value [common])
				common++;

			if (common == len1)
				return str1;
			else if (common == len2)
				return str2;
			else if (common == 0)
				return "";
			else
				return new DataString (str1.Value.Substring (0, common));
		}
	}
}

