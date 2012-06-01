using System;
using System.IO;

namespace FxGqlLib
{
	public interface IGqlCommand
	{
		void Execute (TextWriter outputStream, TextWriter logStream, GqlEngineState gqlEngineState);
	}
}

