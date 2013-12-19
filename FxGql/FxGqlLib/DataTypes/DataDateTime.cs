using System;
using System.Globalization;

namespace FxGqlLib
{
	public struct DataDateTime : IData, IComparable, IComparable<DataDateTime>
	{
		DateTime value;

		public DateTime Value { get { return value; } }

		public override string ToString ()
		{
			return value.ToString ();
		}

		public DataDateTime (DateTime value)
		{
			this.value = value;
		}

		public DataInteger ToDataInteger (CultureInfo cultureInfo)
		{
			throw new ParseTimeConversionException (typeof(DataDateTime), typeof(DataInteger));
		}

		public DataInteger ToDataInteger (CultureInfo cultureInfo, string format)
		{
			throw new ParseTimeConversionException (typeof(DataDateTime), typeof(DataInteger));
		}

		public DataFloat ToDataFloat (CultureInfo cultureInfo)
		{
			throw new ParseTimeConversionException (typeof(DataDateTime), typeof(DataFloat));
		}

		public DataFloat ToDataFloat (CultureInfo cultureInfo, string format)
		{
			throw new ParseTimeConversionException (typeof(DataDateTime), typeof(DataFloat));
		}

		public DataString ToDataString (CultureInfo cultureInfo)
		{
			return this.value.ToString ();
		}

		public DataString ToDataString (CultureInfo cultureInfo, string format)
		{
			return this.value.ToString (format);
		}

		public DataBoolean ToDataBoolean (CultureInfo cultureInfo)
		{
			throw new ParseTimeConversionException (typeof(DataDateTime), typeof(DataBoolean));
		}

		public DataDateTime ToDataDateTime (CultureInfo cultureInfo)
		{
			return this;
		}

		public DataDateTime ToDataDateTime (CultureInfo cultureInfo, string format)
		{
			return this;
		}

		public int CompareTo (object other)
		{
			if (other is DataDateTime)
				return CompareTo ((DataDateTime)other);

			throw new NotSupportedException ();
		}

		public int CompareTo (DataDateTime other)
		{
			return this.value.CompareTo (other.value);
		}

		public static implicit operator DateTime (DataDateTime value)
		{
			return value.value;
		}

		public static implicit operator DataDateTime (DateTime value)
		{
			return new DataDateTime (value);
		}
	}
}

