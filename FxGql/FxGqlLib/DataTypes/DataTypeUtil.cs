using System;

namespace FxGqlLib
{
	public static class DataTypeUtil
	{
		const string STRING = "STRING";
		const string INT = "INT";
		const string FLOAT = "FLOAT";
		const string BOOL = "BOOL";
		const string DATETIME = "DATETIME";

		public static DataType GetDataType (Type type)
		{
			if (type == typeof(DataString))
				return DataType.String;
			if (type == typeof(DataInteger))
				return DataType.Integer;
			if (type == typeof(DataFloat))
				return DataType.Float;
			if (type == typeof(DataBoolean))
				return DataType.Boolean;
			if (type == typeof(DataDateTime))
				return DataType.DateTime;
			throw new InvalidOperationException ("Unknown data type " + type);
		}

		public static string GetDataTypeString (DataType dataType)
		{
			switch (dataType) {
			case DataType.String:
				return STRING;
			case DataType.Integer:
				return INT;
			case DataType.Float:
				return FLOAT;
			case DataType.Boolean:
				return BOOL;
			case DataType.DateTime:
				return DATETIME;
			default:
				throw new InvalidOperationException ("Unknown data type " + dataType);
			}
		}

		public static string GetDataTypeString (Type type)
		{
			return GetDataTypeString (GetDataType (type));
		}

		public static DataType GetTypeFromDataTypeString (string dataTypeString)
		{
			switch (dataTypeString.ToUpperInvariant ()) {
			case STRING:
				return DataType.String;
			case INT:
				return DataType.Integer;
			case FLOAT:
				return DataType.Float;
			case BOOL:
				return DataType.Boolean;
			case DATETIME:
				return DataType.DateTime;
			default:
				throw new InvalidOperationException ("Unknown data type " + dataTypeString);
			}
		}

		public static Type GetTypeFromDataTypeString (DataType dataType)
		{
			switch (dataType) {
			case DataType.String:
				return typeof(DataString);
			case DataType.Integer:
				return typeof(DataInteger);
			case DataType.Float:
				return typeof(DataFloat);
			case DataType.Boolean:
				return typeof(DataBoolean);
			case DataType.DateTime:
				return typeof(DataDateTime);
			default:
				throw new InvalidOperationException ("Unknown data type " + dataType);
			}
		}
	}
}

