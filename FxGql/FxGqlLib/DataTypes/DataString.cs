using System;
using System.Globalization;

namespace FxGqlLib
{
	public struct DataString : IData, IComparable, IComparable<DataString>
	{
		string value;

		public string Value { get { return value; } }

		public override string ToString ()
		{
			return value;
		}

		public DataString (string value)
		{
			this.value = value;
		}

		public DataInteger ToDataInteger ()
		{
			return new DataInteger (long.Parse (this.value));
		}

		public DataInteger ToDataInteger (string format)
		{
			return new DataInteger (long.Parse (this.value));
		}

		public DataFloat ToDataFloat ()
		{
			return new DataFloat (double.Parse (this.value));
		}
		
		public DataFloat ToDataFloat (string format)
		{
			return new DataFloat (double.Parse (this.value));
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
			return new DataBoolean (bool.Parse (this.value));
		}

		public DataDateTime ToDataDateTime ()
		{
			return new DataDateTime (DateTime.Parse (this.value));
		}

		public DataDateTime ToDataDateTime (string format)
		{
			return new DataDateTime (DateTime.ParseExact (this.value, format, System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat));
		}

		public int CompareTo (object other)
		{
			if (other is DataString)
				return CompareTo ((DataString)other);

			throw new NotSupportedException ();
		}

		public int CompareTo (DataString other)
		{
			return this.value.CompareTo (other.value);
		}

		public static implicit operator string (DataString value)
		{
			return value.value;
		}

		public static implicit operator DataString (string value)
		{
			return new DataString (value);
		}

		public void Set (string value)
		{
			this.value = value;
		}
	}
}

