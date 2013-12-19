using System;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace FxGqlLib
{
	public class StateBin
	{
		static ObjectIDGenerator objectIDGenerator = new ObjectIDGenerator ();

		private static long GetId (object obj)
		{
			bool notused;
			return objectIDGenerator.GetId (obj, out notused);
		}

		Dictionary<long, object> state = new Dictionary<long, object> ();

		public bool GetState<T> (object obj, out T val)
		{
			object objVal;
			bool result = state.TryGetValue (GetId (obj), out objVal);
			if (result)
				val = (T)objVal;
			else
				val = default(T);
			return result;
		}

		public void SetState<T> (object obj, T val)
		{
			state [GetId (obj)] = val;
		}
	}
}

