using System;

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

		public DataInteger ToDataInteger ()
		{
			return new DataInteger (this.value ? 1 : 0);
		}

		public DataString ToDataString ()
		{
			return this.value.ToString ();
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

