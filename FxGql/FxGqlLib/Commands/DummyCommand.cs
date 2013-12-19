using System;
using System.IO;

namespace FxGqlLib
{
	public class DummyCommand : IGqlCommand
	{
		public DummyCommand ()
		{
		}

		#region IGqlCommand implementation

		public void Execute (TextWriter outputStream, TextWriter logStream, GqlEngineState gqlEngineState)
		{
		}

		#endregion

	}
}

