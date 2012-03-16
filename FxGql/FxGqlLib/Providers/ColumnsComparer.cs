using System.Collections.Generic;
using System;
using System.Collections;

namespace FxGqlLib
{
	class ColumnsComparer<K> : IComparer<K> where K : ColumnsComparerKey
	{
		IComparer[] comparers;
			
		public ColumnsComparer (Type[] types, StringComparer stringComparer)
		{
			comparers = new IComparer[types.Length];
			for (int i = 0; i < types.Length; i++) {
				if (types [i] == typeof(string))
					comparers [i] = stringComparer;
				else {
					Type comparerType = typeof(Comparer<>).MakeGenericType (types [i]);
					Comparer comparer = Activator.CreateInstance (comparerType) as Comparer;
					comparers [i] = comparer;
				}
			}
		}
			
			#region IComparer[IComparable] implementation
		public int Compare (K x, K y)
		{
			for (int i = 0; i < comparers.Length; i++) {
				int result = comparers [i].Compare (x.Members [i], y.Members [i]);
				if (result != 0)
					return result;
			}
			return 0;
		}
			#endregion
	}
			
	class ColumnsComparerKey : IComparable<ColumnsComparerKey>
	{
		public IComparable[] Members { get; set; }
			
			#region IComparable[Key] implementation
		public int CompareTo (ColumnsComparerKey other)
		{
			for (int i = 0; i < Members.Length; i++) {
				int result = this.Members [i].CompareTo (other.Members [i]);
				if (result != 0)
					return result;
			}
				
			return 0;
		}
			#endregion
	}
}