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

		public override string ToString ()
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

		public void Overwrite (string value)
		{
			String = value;
		}

		public void Overwrite (long value)
		{
			Integer = value;
		}

		public void Overwrite (DateTime value)
		{
			DateTime = value;
		}

		public void Overwrite (bool value)
		{
			Bool = value;
		}

		[Obsolete]
		public void Overwrite (IData value)
		{
			if (Type == typeof(string))
				Overwrite (((DataString)value).Value);
			else if (Type == typeof(long))
				Overwrite (((DataInteger)value).Value);
			else if (Type == typeof(DateTime))
				Overwrite (((DataDateTime)value).Value);
			else if (Type == typeof(bool))
				Overwrite (((DataBoolean)value).Value);
			else
				throw new NotSupportedException ();
		}
	}
}

