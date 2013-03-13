using System;

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

		public DataInteger ToDataInteger ()
		{
			throw new ConversionException (typeof(DataDateTime), typeof(DataInteger));
		}

		public DataInteger ToDataInteger (string format)
		{
			throw new ConversionException (typeof(DataDateTime), typeof(DataInteger));
		}

		public DataFloat ToDataFloat ()
		{
			throw new ConversionException (typeof(DataDateTime), typeof(DataFloat));
		}
		
		public DataFloat ToDataFloat (string format)
		{
			throw new ConversionException (typeof(DataDateTime), typeof(DataFloat));
		}
		
		public DataString ToDataString ()
		{
			return this.value.ToString ();
		}

		public DataString ToDataString (string format)
		{
			return this.value.ToString (format);
		}

		public DataBoolean ToDataBoolean ()
		{
			throw new ConversionException (typeof(DataDateTime), typeof(DataBoolean));
		}

		public DataDateTime ToDataDateTime ()
		{
			return this;
		}

		public DataDateTime ToDataDateTime (string format)
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

