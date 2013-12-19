using System;

namespace FxGqlLib
{
	public class RunTimeConversionException : WarningException
	{
		public RunTimeConversionException (DataType from, DataType to, IData value, Exception inner)
			: base (GetMessage (from, to, value), inner)
		{
		}

		public RunTimeConversionException (Type from, Type to, IData value, Exception inner)
			: base (GetMessage (DataTypeUtil.GetDataType (from), DataTypeUtil.GetDataType (to), value), inner)
		{
		}

		public RunTimeConversionException (Type from, Type to, IData value)
			: this (from, to, value, null)
		{
		}

		static string GetMessage (DataType from, DataType to, object value)
		{
			return string.Format ("Unable to convert {0} value '{1}' to {2}", 
				DataTypeUtil.GetDataTypeString (from), value, DataTypeUtil.GetDataTypeString (to));
		}
	}
}

