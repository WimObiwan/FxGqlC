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

		public DataInteger ToDataInteger (CultureInfo cultureInfo)
		{
			return new DataInteger (long.Parse (this.value, cultureInfo.NumberFormat));
		}

		public DataInteger ToDataInteger (CultureInfo cultureInfo, string format)
		{
			return new DataInteger (long.Parse (this.value, cultureInfo.NumberFormat));
		}

		public DataFloat ToDataFloat (CultureInfo cultureInfo)
		{
			return new DataFloat (double.Parse (this.value, cultureInfo.NumberFormat));
		}
		
		public DataFloat ToDataFloat (CultureInfo cultureInfo, string format)
		{
			return new DataFloat (double.Parse (this.value, cultureInfo.NumberFormat));
		}
		
		public DataString ToDataString (CultureInfo cultureInfo)
		{
			return this;
		}

		public DataString ToDataString (CultureInfo cultureInfo, string format)
		{
			return this;
		}

		public DataBoolean ToDataBoolean (CultureInfo cultureInfo)
		{
			return new DataBoolean (bool.Parse (this.value));
		}

		public DataDateTime ToDataDateTime (CultureInfo cultureInfo)
		{
			return new DataDateTime (DateTime.Parse (this.value, cultureInfo.DateTimeFormat));
		}

		public DataDateTime ToDataDateTime (CultureInfo cultureInfo, string format)
		{
			return new DataDateTime (DateTime.ParseExact (this.value, format, cultureInfo.DateTimeFormat));
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

