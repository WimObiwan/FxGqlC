using System;
using System.Globalization;

namespace FxGqlLib
{
	public struct DataInteger : IData, IComparable, IComparable<DataInteger>
	{
		long value;

		public long Value { get { return value; } }

		public override string ToString ()
		{
			return value.ToString ();
		}

		public DataInteger (long value)
		{
			this.value = value;
		}

		public DataInteger Negate ()
		{
			return new DataInteger (-this.value);
		}

		public DataInteger Add (DataInteger other)
		{
			return new DataInteger (this.value + other.value);
		}

		public DataInteger Substract (DataInteger other)
		{
			return new DataInteger (this.value - other.value);
		}

		public DataInteger Multiply (DataInteger other)
		{
			return new DataInteger (this.value * other.value);
		}

		public DataInteger Divide (DataInteger other)
		{
			return new DataInteger (this.value / other.value);
		}

		public DataInteger Modulo (DataInteger other)
		{
			return new DataInteger (this.value % other.value);
		}

		public DataInteger ToDataInteger (CultureInfo cultureInfo)
		{
			return this;
		}
		
		public DataInteger ToDataInteger (CultureInfo cultureInfo, string format)
		{
			return this;
		}
		
		public DataFloat ToDataFloat (CultureInfo cultureInfo)
		{
			return new DataFloat ((double)this.value);
		}
		
		public DataFloat ToDataFloat (CultureInfo cultureInfo, string format)
		{
			return new DataFloat ((double)this.value);
		}
		
		public DataString ToDataString (CultureInfo cultureInfo)
		{
			return new DataString (this.value.ToString ());
		}

		public DataString ToDataString (CultureInfo cultureInfo, string format)
		{
			return new DataString (this.value.ToString (format));
		}

		public DataBoolean ToDataBoolean (CultureInfo cultureInfo)
		{
			return this.value != 0 ? DataBoolean.True : DataBoolean.False;
		}

		public DataDateTime ToDataDateTime (CultureInfo cultureInfo)
		{
			throw new ParseTimeConversionException (typeof(DataInteger), typeof(DataDateTime));
		}

		public DataDateTime ToDataDateTime (CultureInfo cultureInfo, string format)
		{
			throw new ParseTimeConversionException (typeof(DataInteger), typeof(DataDateTime));
		}

		public int CompareTo (object other)
		{
			if (other is DataInteger)
				return CompareTo ((DataInteger)other);

			throw new NotSupportedException ();
		}

		public int CompareTo (DataInteger other)
		{
			return this.value.CompareTo (other.value);
		}

		public static implicit operator long (DataInteger value)
		{
			return value.value;
		}

		public static implicit operator DataInteger (long value)
		{
			return new DataInteger (value);
		}
	}
}

