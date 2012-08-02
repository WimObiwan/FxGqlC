using System.Collections.Generic;
using System;
using System.Collections;

namespace FxGqlLib
{
	class ColumnsComparer<K> : IComparer<K>, IEqualityComparer<K> where K : ColumnsComparerKey
	{
		readonly IComparer<IData>[] comparers;
		readonly bool[] descending;
		readonly int[] fixedColumns;

		public int[] FixedColumns { get { return fixedColumns; } }
			
		public ColumnsComparer (Type[] types, DataComparer dataComparer)
			: this(types, new bool[types.Length], null, dataComparer)
		{
		}
		
		public ColumnsComparer (Type[] types, int[] fixedColumns, DataComparer dataComparer)
			: this(types, new bool[types.Length], fixedColumns, dataComparer)
		{
		}
		
		public ColumnsComparer (Type[] types, bool[] descending, int[] fixedColumns, DataComparer dataComparer)
		{
			comparers = new IComparer<IData>[types.Length];
			this.descending = descending;
			this.fixedColumns = fixedColumns;
			
			//TODO: Check array sizes
			for (int i = 0; i < types.Length; i++) {
				comparers [i] = dataComparer;
				/*if (types [i] == typeof(DataString))
					comparers [i] = stringComparer;
				else {
					Type comparerType = typeof(Comparer<>).MakeGenericType (types [i]);
					object obj = comparerType.GetProperty ("Default").GetValue (null, null);
					//Comparer comparer = obj as Comparer;
					//Comparer comparer = Activator.CreateInstance (comparerType) as Comparer;
					comparers [i] = obj as IComparer;
				}*/
			}
		}
			
		#region IComparer[IComparable] implementation
		public int Compare (K x, K y)
		{
			for (int i = 0; i < comparers.Length; i++) {
				int result = comparers [i].Compare (x.Members [i], y.Members [i]);
				if (result != 0)
					return descending [i] ? -result : result;
			}
			return 0;
		}
		#endregion

		#region IEqualityComparer[K] implementation
		public bool Equals (K x, K y)
		{
			return Compare (x, y) == 0;
		}
		
		public int GetHashCode (K x)
		{
			int hash = x.Members.Length;
			foreach (var t in x.Members) {
				hash *= 17;
				hash = hash + t.GetHashCode ();
			}
			return hash;
		}
		#endregion
	}
			
	class ColumnsComparerKey : IComparable<ColumnsComparerKey>
	{
		public IData[] Members { get; set; }
			
		public ColumnsComparerKey ()
		{
		}

		public ColumnsComparerKey (IData[] members)
		{
			Members = members;
		}

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