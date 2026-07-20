using DemoContentLoader;
using Demos.GL;
using System;
using System.Diagnostics;

namespace Demos.SpecializedTests;

static class HeadlessTest
{
    public static void Test<T>(ContentArchive content, int runCount, int warmUpFrames, int frameCount) where T : DemoBase, new()
    {
        double[] runFrameTimes = new double[runCount];
        ulong maximumMemoryUsedInMainPool = 0;
        ulong maximumMemoryUsedInThreadPools = 0;
        ulong maximumMemoryUsedInPools = 0;
        for (int runIndex = 0; runIndex < runCount; ++runIndex)
        {
            T demo = new();
            demo.Initialize(content, new DemoRenderer.Camera(1, 1, 1, 1));
            GC.Collect(3, GCCollectionMode.Forced, true, true);
            for (int i = 0; i < warmUpFrames; ++i)
            {
                demo.Update(null, null, null, DemoBase.TimestepDuration);
            }
            Console.WriteLine($"Warmup {runIndex} complete");
            double time = 0;
            int largestOverlapCount = 0;
            Console.Write("Completed frames: ");
            for (int i = 0; i < frameCount; ++i)
            {
                //CacheBlaster.Blast();
                long start = Stopwatch.GetTimestamp();
                demo.Update(null, null, null, DemoBase.TimestepDuration);
                long end = Stopwatch.GetTimestamp();
                time += (end - start) / (double)Stopwatch.Frequency;
                if (i % 32 == 0)
                    Console.Write($"{i}, ");
                ulong mainPoolSize = demo.BufferPool.GetTotalAllocatedByteCount();
                ulong threadPoolSize = demo.ThreadDispatcher.WorkerPools.GetTotalAllocatedByteCount();
                ulong totalPoolSize = mainPoolSize + threadPoolSize;
                if (totalPoolSize > maximumMemoryUsedInPools)
                {
                    maximumMemoryUsedInPools = totalPoolSize;
                    maximumMemoryUsedInMainPool = mainPoolSize;
                    maximumMemoryUsedInThreadPools = threadPoolSize;
                }

            }
            Console.WriteLine();
            double frameTime = time / frameCount;
            Console.WriteLine($"Time per frame (ms): {1e3 * frameTime}, maximum overlap count: {largestOverlapCount}");
            runFrameTimes[runIndex] = frameTime;
            demo.Dispose();
        }
        double min = double.MaxValue;
        double max = double.MinValue;
        double sum = 0.0;
        double sumOfSquares = 0.0;
        for (int runIndex = 0; runIndex < runCount; ++runIndex)
        {
            double time = runFrameTimes[runIndex];
            min = Math.Min(time, min);
            max = Math.Max(time, max);
            sum += time;
            sumOfSquares += time * time;
        }
        double average = sum / runCount;
        double stdDev = Math.Sqrt(sumOfSquares / runCount - average * average);
        Console.WriteLine($"Average (ms): {average * 1e3}");
        Console.WriteLine($"Min, max (ms): {min * 1e3}, {max * 1e3}");
        Console.WriteLine($"Std Dev (ms): {stdDev * 1e3}");
        Console.WriteLine($"Maximum memory used in pools: {maximumMemoryUsedInPools} (main: {maximumMemoryUsedInMainPool}, threads: {maximumMemoryUsedInThreadPools})");
    }
}
