using System;

namespace FxGqlLib
{
	public static class DataConversion
	{
		[Obsolete]
		public static Y As<Y> (IData data)
		{
			if (typeof(Y) == typeof(DataInteger))
				return (Y)(object)data.ToDataInteger ();
			else if (typeof(Y) == typeof(DataString))
				return (Y)(object)data.ToDataString ();
			else if (typeof(Y) == typeof(string))
				return (Y)(object)AsString (data);
			throw new ConversionException (data.GetType (), typeof(Y));
		}

		[Obsolete]
		public static long AsLong (IData data)
		{
			DataInteger d = data.ToDataInteger ();
			return d.Value;
		}

		[Obsolete]
		public static string AsString (IData data)
		{
			DataString d = data.ToDataString ();
			return d.ToString ();
		}
	}
}

