using System;
using System.Globalization;

namespace FxGqlLib
{
	public struct DataString : IData, IComparable, IComparable<DataString>
	{
		string value;
		char[] buffer;
		int start;
		int len;

		public string Value { 
			get { 
				if (value == null && buffer != null)
					value = new string (buffer, start, len);
				return value;
			}
		}

		public override string ToString ()
		{
			return Value;
		}

		public DataString (string value)
		{
			this.value = value;
			this.buffer = null;
			this.start = -1;
			this.len = -1;
		}

		public DataInteger ToDataInteger ()
		{
			return new DataInteger (long.Parse (Value));
		}

		public DataInteger ToDataInteger (string format)
		{
			return new DataInteger (long.Parse (Value));
		}

		public DataString ToDataString ()
		{
			return this;
		}

		public DataString ToDataString (string format)
		{
			return this;
		}

		public DataBoolean ToDataBoolean ()
		{
			return new DataBoolean (bool.Parse (Value));
		}

		public DataDateTime ToDataDateTime ()
		{
			return new DataDateTime (DateTime.Parse (Value));
		}

		public DataDateTime ToDataDateTime (string format)
		{
			return new DataDateTime (DateTime.ParseExact (Value, format, System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat));
		}

		public int CompareTo (object other)
		{
			if (other is DataString)
				return CompareTo ((DataString)other);

			throw new NotSupportedException ();
		}

		public int CompareTo (DataString other)
		{
			return Value.CompareTo (other.value);
		}

		public static implicit operator string (DataString value)
		{
			return value.Value;
		}

		public static implicit operator DataString (string value)
		{
			return new DataString (value);
		}

		public void Set (string value)
		{
			this.value = value;
			this.buffer = null;
			this.start = -1;
			this.len = -1;
		}

		public void Set (char[] buffer, int start, int len)
		{
			this.value = null;
			this.buffer = buffer;
			this.start = start;
			this.len = len;
		}
	}
}

