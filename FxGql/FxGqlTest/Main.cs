using System;

namespace FxGqlTest
{
	class MainClass
	{
		public static int Main (string[] args)
		{
			GqlSamplesTest gqlSamplesTest = new GqlSamplesTest ();

			bool result = true;
#if DEBUG
			result = gqlSamplesTest.RunDevelop ();
			//result = gqlSamplesTest.Run ();
#else
			result = gqlSamplesTest.Run ();
#endif
			var oldColor = Console.ForegroundColor;
			if (result) {
				Console.ForegroundColor = ConsoleColor.Green;
				Console.WriteLine ("***** SUCCEEDED *****");
			} else {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine ("***** FAILED *****");
			}
			Console.ForegroundColor = oldColor;

			return result ? 0 : 1;
		}
	}
}
