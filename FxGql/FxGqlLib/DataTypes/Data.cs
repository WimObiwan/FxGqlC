using System;
using System.Globalization;

namespace FxGqlLib
{
	public interface IData : IComparable
	{
		DataInteger ToDataInteger (CultureInfo ci);

		DataInteger ToDataInteger (CultureInfo ci, string format);

		DataFloat ToDataFloat (CultureInfo ci);

		DataFloat ToDataFloat (CultureInfo ci, string format);

		DataString ToDataString (CultureInfo ci);

		DataString ToDataString (CultureInfo ci, string format);

		DataBoolean ToDataBoolean (CultureInfo ci);

		DataDateTime ToDataDateTime (CultureInfo ci);

		DataDateTime ToDataDateTime (CultureInfo ci, string format);
	}
}

