using System;

namespace FxGqlLib
{
	public interface IObsoleteDataType<T>
	{
		T Value { get; }
	}
}

