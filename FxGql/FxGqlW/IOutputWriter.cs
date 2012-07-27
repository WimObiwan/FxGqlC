using System;

namespace FxGqlW
{
	public interface IOutputWriter
	{
		void WriteLine (string text);
		void WriteErrorLine (string text);
	}
}

