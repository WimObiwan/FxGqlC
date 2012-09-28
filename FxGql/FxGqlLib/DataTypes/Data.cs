using System;

namespace FxGqlLib
{
	public interface IData : IComparable
	{
		DataInteger ToDataInteger ();
		DataInteger ToDataInteger (string format);
		DataString ToDataString ();
		DataString ToDataString (string format);
		DataBoolean ToDataBoolean ();
		DataDateTime ToDataDateTime ();
		DataDateTime ToDataDateTime (string format);

	}

	public struct NewData
	{
		public Type Type;
		public string String;
		public long Integer;
		public DateTime DateTime;
		public bool Bool;

		public string ToString ()
		{
			if (Type == typeof(string))
				return String;
			else if (Type == typeof(long))
				return Integer.ToString ();
			else if (Type == typeof(DateTime))
				return DateTime.ToString ();
			else if (Type == typeof(bool))
				return Bool.ToString ();
			else
				throw new NotSupportedException ();
		}
	}
}

