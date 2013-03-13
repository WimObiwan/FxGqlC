using System;

namespace FxGqlLib
{
	public struct DataFloat : IData, IComparable, IComparable<DataFloat>
	{
		double value;
		
		public double Value { get { return value; } }
		
		public override string ToString ()
		{
			return value.ToString ();
		}
		
		public DataFloat (double value)
		{
			this.value = value;
		}
		
		public DataFloat Negate ()
		{
			return new DataFloat (-this.value);
		}
		
		public DataFloat Add (DataFloat other)
		{
			return new DataFloat (this.value + other.value);
		}
		
		public DataFloat Substract (DataFloat other)
		{
			return new DataFloat (this.value - other.value);
		}
		
		public DataFloat Multiply (DataFloat other)
		{
			return new DataFloat (this.value * other.value);
		}
		
		public DataFloat Divide (DataFloat other)
		{
			return new DataFloat (this.value / other.value);
		}

		public DataInteger ToDataInteger ()
		{
			return (int)this.value;
		}
		
		public DataInteger ToDataInteger (string format)
		{
			return (int)this.value;
		}
		
		public DataFloat ToDataFloat ()
		{
			return this;
		}
		
		public DataFloat ToDataFloat (string format)
		{
			return this;
		}
		
		public DataString ToDataString ()
		{
			return new DataString (this.value.ToString ());
		}
		
		public DataString ToDataString (string format)
		{
			return new DataString (this.value.ToString (format));
		}
		
		public DataBoolean ToDataBoolean ()
		{
			throw new ConversionException (typeof(DataFloat), typeof(DataDateTime));
		}
		
		public DataDateTime ToDataDateTime ()
		{
			throw new ConversionException (typeof(DataFloat), typeof(DataDateTime));
		}
		
		public DataDateTime ToDataDateTime (string format)
		{
			throw new ConversionException (typeof(DataFloat), typeof(DataDateTime));
		}
		
		public int CompareTo (object other)
		{
			if (other is DataFloat)
				return CompareTo ((DataFloat)other);
			
			throw new NotSupportedException ();
		}
		
		public int CompareTo (DataFloat other)
		{
			return this.value.CompareTo (other.value);
		}
		
		public static implicit operator double (DataFloat value)
		{
			return value.value;
		}
		
		public static implicit operator DataFloat (double value)
		{
			return new DataFloat (value);
		}
	}
}

