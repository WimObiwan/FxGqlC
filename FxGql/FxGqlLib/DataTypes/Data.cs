using System;

namespace FxGqlLib
{
	public interface IData : IComparable
	{
		DataInteger ToDataInteger ();
		DataString ToDataString ();
		DataBoolean ToDataBoolean ();
	}
}

