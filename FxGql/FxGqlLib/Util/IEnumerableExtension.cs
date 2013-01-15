using System;
using System.Collections.Generic;
using System.Text;

namespace FxGqlLib
{
	public static class IEnumerableExtensions
	{
		public static string Enlist<T> (this IEnumerable<T> e, Func<T, string> func)
		{
			bool first = true;
			StringBuilder sb = new StringBuilder ();
			foreach (var item in e) {
				if (first)
					first = false;
				else
					sb.Append (';');
				sb.Append (func (item));
			}
			return sb.ToString ();
		}
	}
}
