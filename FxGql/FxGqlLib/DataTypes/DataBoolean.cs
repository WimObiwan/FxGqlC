using System;
using System.Globalization;

namespace FxGqlLib
{
	public struct DataBoolean : IData, IComparable, IComparable<DataBoolean>
	{
		bool value;

		public bool Value { get { return value; } }

		public override string ToString ()
		{
			return value.ToString ();
		}

		public DataBoolean (bool value)
		{
			this.value = value;
		}

		public DataInteger ToDataInteger (CultureInfo cultureInfo)
		{
			return new DataInteger (this.value ? 1 : 0);
		}

		public DataInteger ToDataInteger (CultureInfo cultureInfo, string format)
		{
			return new DataInteger (this.value ? 1 : 0);
		}

		public DataFloat ToDataFloat (CultureInfo cultureInfo)
		{
			throw new ParseTimeConversionException (typeof(DataBoolean), typeof(DataFloat));
		}
		
		public DataFloat ToDataFloat (CultureInfo cultureInfo, string format)
		{
			throw new ParseTimeConversionException (typeof(DataBoolean), typeof(DataFloat));
		}
		
		public DataString ToDataString (CultureInfo cultureInfo)
		{
			return this.value.ToString ();
		}

		public DataString ToDataString (CultureInfo cultureInfo, string format)
		{
			return this.value.ToString ();
		}

		public DataBoolean ToDataBoolean (CultureInfo cultureInfo)
		{
			return this;
		}

		public DataDateTime ToDataDateTime (CultureInfo cultureInfo)
		{
			throw new ParseTimeConversionException (typeof(DataBoolean), typeof(DataDateTime));
		}

		public DataDateTime ToDataDateTime (CultureInfo cultureInfo, string format)
		{
			throw new ParseTimeConversionException (typeof(DataBoolean), typeof(DataDateTime));
		}

		public int CompareTo (object other)
		{
			if (other is DataBoolean)
				return CompareTo ((DataBoolean)other);

			throw new NotSupportedException ();
		}

		public int CompareTo (DataBoolean other)
		{
			return this.value.CompareTo (other.value);
		}

		public static implicit operator bool (DataBoolean value)
		{
			return value.value;
		}

		public static implicit operator DataBoolean (bool value)
		{
			return new DataBoolean (value);
		}

		public static DataBoolean trueValue = new DataBoolean (true);
		public static DataBoolean falseValue = new DataBoolean (false);
		public static DataBoolean True { get { return trueValue; } }
		public static DataBoolean False { get { return falseValue; } }
	}
}

