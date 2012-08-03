using System;

namespace FxGqlLib
{
	static class OperatorHelper
	{
		public static Func<DataString, DataString, DataBoolean> GetStringComparer (string operand, bool negate, StringComparison stringComparison)
		{
			Func<int, bool> comparer = GetComparer (operand);
			if (negate)
				return (a, b) => !comparer (string.Compare (a, b, stringComparison));
			else
				return (a, b) => comparer (string.Compare (a, b, stringComparison));
		}
		
		public static Func<DataInteger, DataInteger, DataBoolean> GetIntegerComparer (string operand, bool negate)
		{
			Func<int, bool> comparer = GetComparer (operand);
			if (negate)
				return (a, b) => !comparer (a.CompareTo (b));
			else
				return (a, b) => comparer (a.CompareTo (b));
		}

		public static Func<DataBoolean, DataBoolean, DataBoolean> GetBooleanComparer (string operand, bool negate)
		{
			Func<int, bool> comparer = GetComparer (operand);
			if (negate)
				return (a, b) => !comparer (a.CompareTo (b));
			else
				return (a, b) => comparer (a.CompareTo (b));
		}

		public static Func<int, bool> GetComparer (string operand)
		{
			switch (operand) {
			case "T_EQUAL":
				return a => a == 0;
			case "T_NOTEQUAL":
				return a => a != 0;
			case "T_LESS":
				return a => a < 0;
			case "T_NOTLESS":
				return a => a >= 0;
			case "T_GREATER":
				return a => a > 0;
			case "T_NOTGREATER":
				return a => a <= 0;
			default:
				throw new Exception (string.Format ("Unknown operator {0}", operand));
			}
		}
	}
}

