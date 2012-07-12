using System;

namespace FxGqlLib
{
	public static class DataConversion
	{
		[Obsolete]
		public static Y As<Y> (IData data)
		{
			if (typeof(Y) == typeof(DataInteger))
				return (Y)(object)AsDataInteger (data);
			else if (typeof(Y) == typeof(string))
				return (Y)(object)AsString (data);
			throw new ConversionException (data.GetType (), typeof(Y));
		}

		[Obsolete]
		public static long AsLong (IData data)
		{
			DataInteger dataInteger = AsDataInteger (data);
			return dataInteger.Value;
		}

		[Obsolete]
		public static string AsString (IData data)
		{
			DataInteger dataInteger = AsDataInteger (data);
			return dataInteger.ToString ();
		}

		public static DataInteger AsDataInteger (IData data)
		{
			if (data is DataInteger)
				return (DataInteger)data;

			throw new ConversionException (data.GetType (), typeof(DataInteger));
		}
	}
}

