using System;

namespace FxGqlTest
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			GqlSamplesTest gqlSamplesTest = new GqlSamplesTest ();
#if DEBUG
			gqlSamplesTest.RunDevelop ();
			//gqlSamplesTest.Run ();
#else
			gqlSamplesTest.Run ();
#endif
		}
	}
}
