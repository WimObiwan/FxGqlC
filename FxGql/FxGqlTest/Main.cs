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
			gqlSamplesTest.RunDevelop ();
			//gqlSamplesTest.Run ();
#else
			result = gqlSamplesTest.Run ();
#endif

			return result ? 0 : 1;
		}
	}
}
