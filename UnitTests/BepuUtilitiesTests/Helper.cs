using System;
using System.Diagnostics;

namespace BEPUutilitiesTests
{
    public static class Helper
    {
        public static void Test(string testName, Func<int, float> function, int benchmarkIterations = 100000000, int warmupIterations = 8192)
        {
            GC.Collect();
            function(warmupIterations);
            long start = Stopwatch.GetTimestamp();
            float accumulator = function(benchmarkIterations);
            long end = Stopwatch.GetTimestamp();
            Console.WriteLine($"{testName} time: {(end - start) / (double)Stopwatch.Frequency}");
        }
    }
}
