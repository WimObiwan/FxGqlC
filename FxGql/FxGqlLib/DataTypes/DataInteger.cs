using System;

namespace FxGqlLib
{
	public struct DataInteger : IData, IObsoleteDataType<long>
	{
		long value;

		[Obsolete]
		public long Value { get { return value; } }

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

		public int CompareTo (object other)
		{
			if (other is DataInteger)
				return this.value.CompareTo (((DataInteger)other).value);

			throw new NotSupportedException ();
		}

		public static implicit operator long (DataInteger value)
		{
			return value.value;
		}
	}
}

