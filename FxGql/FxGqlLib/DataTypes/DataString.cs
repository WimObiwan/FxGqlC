using System;

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

		public DataString ToDataString ()
		{
			return this;
		}

		public DataBoolean ToDataBoolean ()
		{
			return new DataBoolean (bool.Parse (this.value));
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

