using BepuUtilities;
using BepuUtilities.Collections;
using BepuUtilities.Memory;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics.Trees;

namespace Demos.SpecializedTests;

public unsafe static class TreeTest
{
    public static void Test()
    {
        BufferPool pool = new();
        Tree tree = new(pool, 128);

        const int leafCountAlongXAxis = 11;
        const int leafCountAlongYAxis = 13;
        const int leafCountAlongZAxis = 15;
        var leafCount = leafCountAlongXAxis * leafCountAlongYAxis * leafCountAlongZAxis;
        pool.Take<BoundingBox>(leafCount, out Buffer<BoundingBox> leafBounds);

        const float boundsSpan = 2;
        const float spanRange = 2;
        const float boundsSpacing = 3;
        Random random = new(5);
        for (int i = 0; i < leafCountAlongXAxis; ++i)
        {
            for (int j = 0; j < leafCountAlongYAxis; ++j)
            {
                for (int k = 0; k < leafCountAlongZAxis; ++k)
                {
                    var index = leafCountAlongXAxis * leafCountAlongYAxis * k + leafCountAlongXAxis * j + i;
                    leafBounds[index].Min = new Vector3(i, j, k) * boundsSpacing;
                    leafBounds[index].Max = leafBounds[index].Min + new Vector3(boundsSpan) +
                        spanRange * new Vector3(random.NextSingle(), random.NextSingle(), random.NextSingle());

                }
            }
        }

        var prebuiltCount = Math.Max(leafCount / 2, 1);

        tree.SweepBuild(pool, leafBounds.Slice(prebuiltCount));
        tree.Validate();


        for (int i = prebuiltCount; i < leafCount; ++i)
        {
            tree.Add(leafBounds[i], pool);
        }
        tree.Validate();

        pool.TakeAtLeast<int>(leafCount, out Buffer<int> handleToLeafIndex);
        pool.TakeAtLeast<int>(leafCount, out Buffer<int> leafIndexToHandle);
        for (int i = 0; i < leafCount; ++i)
        {
            handleToLeafIndex[i] = i;
            leafIndexToHandle[i] = i;
        }

        const int iterations = 100000;
        const int maximumChangesPerIteration = 20;

        ThreadDispatcher threadDispatcher = new(Environment.ProcessorCount);
        Tree.RefitAndRefineMultithreadedContext refineContext = new();
        Tree.MultithreadedSelfTest<OverlapHandler> selfTestContext = new(pool);
        OverlapHandler[] overlapHandlers = new OverlapHandler[threadDispatcher.ThreadCount];
        Action<int> pairTestAction = selfTestContext.PairTest;
        QuickList<int> removedLeafHandles = new(leafCount, pool);
        for (int i = 0; i < iterations; ++i)
        {
            var changeCount = random.Next(maximumChangesPerIteration);
            for (int j = 0; j <= changeCount; ++j)
            {
                var addedFraction = tree.LeafCount / (float)leafCount;
                if (random.NextDouble() < addedFraction)
                {
                    //Remove a leaf.
                    var leafIndexToRemove = random.Next(tree.LeafCount);
                    var handleToRemove = leafIndexToHandle[leafIndexToRemove];
                    var movedLeafIndex = tree.RemoveAt(leafIndexToRemove);
                    if (movedLeafIndex >= 0)
                    {
                        var movedHandle = leafIndexToHandle[movedLeafIndex];
                        handleToLeafIndex[movedHandle] = leafIndexToRemove;
                        leafIndexToHandle[leafIndexToRemove] = movedHandle;
                        leafIndexToHandle[movedLeafIndex] = -1;
                    }
                    else
                    {
                        //The removed leaf was the last one. This leaf index is no longer associated with any existing leaf.
                        leafIndexToHandle[leafIndexToRemove] = -1;
                    }
                    handleToLeafIndex[handleToRemove] = -1;

                    removedLeafHandles.AddUnsafely(handleToRemove);

                    tree.Validate();
                }
                else
                {
                    //Add a leaf.
                    var indexInRemovedList = random.Next(removedLeafHandles.Count);
                    var handleToAdd = removedLeafHandles[indexInRemovedList];
                    removedLeafHandles.FastRemoveAt(indexInRemovedList);
                    var leafIndex = tree.Add(leafBounds[handleToAdd], pool);
                    leafIndexToHandle[leafIndex] = handleToAdd;
                    handleToLeafIndex[handleToAdd] = leafIndex;

                    tree.Validate();
                }
            }

            tree.Refit();
            tree.Validate();

            tree.RefitAndRefine(pool, i);
            tree.Validate();

            OverlapHandler handler = new();
            tree.GetSelfOverlaps(ref handler);
            tree.Validate();

            refineContext.RefitAndRefine(ref tree, pool, threadDispatcher, i);
            tree.Validate();
            for (int k = 0; k < threadDispatcher.ThreadCount; ++k)
            {
                overlapHandlers[k] = new OverlapHandler();
            }
            selfTestContext.PrepareJobs(ref tree, overlapHandlers, threadDispatcher.ThreadCount);
            threadDispatcher.DispatchWorkers(pairTestAction);
            selfTestContext.CompleteSelfTest();
            tree.Validate();

            if (i % 50 == 0)
            {
                Console.WriteLine($"Cost: {tree.MeasureCostMetric()}");
                Console.WriteLine($"Cache Quality: {tree.MeasureCacheQuality()}");
                Console.WriteLine($"Overlap Count: {handler.OverlapCount}");
            }
        }

        threadDispatcher.Dispose();
        pool.Clear();


    }

    struct OverlapHandler : IOverlapHandler
    {
        public int OverlapCount;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Handle(int indexA, int indexB)
        {
            ++OverlapCount;
        }
    }

}
