using System;
using System.Collections.Generic;
using System.Linq;

namespace FxGqlTest
{
	class MainClass
	{
		public static int Main (string[] args)
		{
			using (GqlSamplesTest gqlSamplesTest = new GqlSamplesTest ()) {
				bool result = true;
				if (args.Length > 0 && args [0] == "develop") {
					result = gqlSamplesTest.RunDevelop ();
				} else if (args.Length > 0 && args [0] == "performance") {
					int count;
					if (args.Length <= 1 || !int.TryParse (args [1], out count))
						count = 1;
					var processorTimeList = new List<long> ();
					var stopwatchList = new List<long> ();
					while (count-- > 0) {
						gqlSamplesTest.engineHash.Reset ();
						gqlSamplesTest.engineOutput.Reset ();

						gqlSamplesTest.Performance = true;
						TimeSpan processorTime = System.Diagnostics.Process.GetCurrentProcess ().TotalProcessorTime;
						System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew ();
						result = gqlSamplesTest.Run ();
						stopwatch.Stop ();
						processorTime = System.Diagnostics.Process.GetCurrentProcess ().TotalProcessorTime - processorTime;
						Console.WriteLine ("Elapsed: {0}, CPU: {1} ({2:0.00}%)", stopwatch.Elapsed, processorTime, processorTime.Ticks * 100.0 / stopwatch.Elapsed.Ticks);
						processorTimeList.Add (processorTime.Ticks);
						stopwatchList.Add (stopwatch.Elapsed.Ticks);
					}

					if (processorTimeList.Count > 1) {
						//        (F)       (S)
						//  orig skip take left
						//     1    0    1    0
						//     2    0    1    1
						//     3    1    1    1
						//     4    1    2    1
						//     5    1    2    2
						//     6    2    2    2
						int skip = stopwatchList.Count / 3;
						int take = (stopwatchList.Count + 2) / 3;
						stopwatchList = stopwatchList.OrderBy (p => p).Skip (skip).Take (take).ToList ();
						processorTimeList = processorTimeList.OrderBy (p => p).Skip (skip).Take (take).ToList ();
						TimeSpan stopwatch = new TimeSpan ((long)stopwatchList.Average ());
						TimeSpan processorTime = new TimeSpan ((long)processorTimeList.Average ());
						Console.WriteLine ("TOTAL  : {0}, CPU: {1} ({2:0.00}%)", stopwatch, processorTime, processorTime.Ticks * 100.0 / stopwatch.Ticks);
					}
				} else {
					result = gqlSamplesTest.Run ();
				}

				var oldColor = Console.ForegroundColor;
				if (result) {
					Console.ForegroundColor = ConsoleColor.DarkGreen;
					Console.WriteLine ("***** SUCCEEDED *****");
				} else {
					Console.ForegroundColor = ConsoleColor.DarkRed;
					Console.WriteLine ("***** FAILED *****");
				}
				Console.ForegroundColor = oldColor;

				return result ? 0 : 1;
			}
		}
	}
}
