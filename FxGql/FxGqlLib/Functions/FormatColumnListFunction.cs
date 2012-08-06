using System;
using System.Linq;
using System.Collections.Generic;

namespace FxGqlLib
{
	public class FormatColumnListFunction : Expression<DataString>
	{
		readonly string separator;
		readonly int[] columnSizes;
		
		public FormatColumnListFunction (string separator)
			: this(separator, null)
		{
		}

		public FormatColumnListFunction (string separator, int[] columnSizes)
		{
			this.separator = separator;
			this.columnSizes = columnSizes;
		}
				
		#region implemented abstract members of FxGqlLib.Expression[System.String]
		public override DataString Evaluate (GqlQueryState gqlQueryState)
		{
			IEnumerable<string> columns = gqlQueryState.Record.Columns.Select (p => p.ToString ());
			return Evaluate (columns);
		}
		#endregion

		public DataString Evaluate (IEnumerable<string> columns)
		{
			if (columnSizes != null) {
				string[] columns2 = columns.ToArray ();
				for (int i = 0; i < Math.Min (columns2.Length, columnSizes.Length); i++)
					if (columnSizes [i] != 0) {
						if (columns2 [i].Length > Math.Abs (columnSizes [i])) {
							columns2 [i] = columns2 [i].Substring (0, Math.Abs (columnSizes [i]) - 1) + '~';
						} else {
							if (columnSizes [i] > 0)
								columns2 [i] = columns2 [i].PadRight (columnSizes [i]);
							else //if (columnSizes [i] < 0)
								columns2 [i] = columns2 [i].PadLeft (-columnSizes [i]);
						}
					}
				columns = columns2;
			}
			return new DataString (string.Concat (new SeparatorEnumerable<string> (columns, separator)));
		}

		class SeparatorEnumerable<T> : IEnumerable<T>
		{
			T separator;
			IEnumerable<T> enumerable;

			public SeparatorEnumerable (IEnumerable<T> enumerable, T separator)
			{
				this.separator = separator;
				this.enumerable = enumerable;
			}

			#region IEnumerable implementation
			public System.Collections.IEnumerator GetEnumerator ()
			{
				IEnumerator<T> enumerator = enumerable.GetEnumerator ();
				bool first = true;
				while (enumerator.MoveNext()) {
					if (first) 
						first = false;
					else
						yield return separator;
					yield return enumerator.Current;
				}
			}
			#endregion

			#region IEnumerable implementation
			IEnumerator<T> IEnumerable<T>.GetEnumerator ()
			{
				IEnumerator<T> enumerator = enumerable.GetEnumerator ();
				bool first = true;
				while (enumerator.MoveNext()) {
					if (first) 
						first = false;
					else
						yield return separator;
					yield return enumerator.Current;
				}
			}
			#endregion
		}
	}
}

