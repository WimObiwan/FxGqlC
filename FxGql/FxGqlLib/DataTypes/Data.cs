using System;

namespace FxGqlLib
{
	public interface IData : IComparable
	{
		DataInteger ToDataInteger ();
		DataInteger ToDataInteger (string format);
		DataString ToDataString ();
		DataString ToDataString (string format);
		DataBoolean ToDataBoolean ();
		DataDateTime ToDataDateTime ();
		DataDateTime ToDataDateTime (string format);

	}
}

