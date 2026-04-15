using BepuUtilities;
using BepuUtilities.Memory;
using BepuPhysics.Trees;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Demos.SpecializedTests;

public static class IntertreeThreadingTests
{
    static void GetRandomLocation(Random random, ref BoundingBox locationBounds, out Vector3 location)
    {
        location = (locationBounds.Max - locationBounds.Min) * new Vector3(random.NextSingle(), random.NextSingle(), random.NextSingle()) + locationBounds.Min;
    }
    struct OverlapHandler : IOverlapHandler
    {
        public List<(int a, int b)> Pairs;
        public void Handle(int indexA, int indexB)
        {
            Pairs.Add((indexA, indexB));
        }
    }

    static void GetBoundsForLeaf(in Tree tree, int leafIndex, out BoundingBox bounds)
    {
        ref Leaf leaf = ref tree.Leaves[leafIndex];
        ref Node node = ref tree.Nodes[leaf.NodeIndex];
        bounds = leaf.ChildIndex == 0 ? new BoundingBox(node.A.Min, node.A.Max) : new BoundingBox(node.B.Min, node.B.Max);
    }

    static void SortPairs(List<(int a, int b)> pairs)
    {
        for (int i = 0; i < pairs.Count; ++i)
        {
            if (pairs[i].b < pairs[i].a)
            {
                pairs[i] = (pairs[i].b, pairs[i].a);
            }
        }
        Comparison<(int, int)> comparison = (a, b) =>
        {
            var combinedA = ((ulong)a.Item1 << 32) | ((uint)a.Item2);
            var combinedB = ((ulong)b.Item1 << 32) | ((uint)b.Item2);
            return combinedA.CompareTo(combinedB);
        };
        pairs.Sort(comparison);
    }

    unsafe static void TestTrees(BufferPool pool, IThreadDispatcher threadDispatcher, Random random)
    {
        Tree treeA = new(pool, 1);
        Tree treeB = new(pool, 1);

        BoundingBox aBounds = new(new Vector3(-40, 0, -40), new Vector3(40, 0, 40));
        Vector3 aOffset = new(3f, 3f, 3f);
        var aCount = 1024;
        BoundingBox bBounds = new(new Vector3(-5, -2, -5), new Vector3(5, 2, 5));
        Vector3 bOffset = new(0.5f, 0.5f, 0.5f);
        var bCount = 3;
        for (int i = 0; i < aCount; ++i)
        {
            GetRandomLocation(random, ref aBounds, out Vector3 center);
            BoundingBox bounds = new(center - aOffset, center + aOffset);
            treeA.Add(bounds, pool);
        }
        for (int i = 0; i < bCount; ++i)
        {
            GetRandomLocation(random, ref bBounds, out Vector3 center);
            BoundingBox bounds = new(center - bOffset, center + bOffset);
            treeB.Add(bounds, pool);
        }
        
        {
            var indexToRemove = 1;
            GetBoundsForLeaf(treeB, indexToRemove, out BoundingBox removedBounds);
            treeB.RemoveAt(indexToRemove);
            treeA.Add(removedBounds, pool);
        }

        OverlapHandler singleThreadedResults = new() { Pairs = new List<(int a, int b)>() };
        treeA.GetOverlaps(ref treeB, ref singleThreadedResults);
        SortPairs(singleThreadedResults.Pairs);
        for (int i = 0; i < 10; ++i)
        {
            treeA.RefitAndRefine(pool, i);
            treeB.RefitAndRefine(pool, i);
        }
        treeA.Validate();
        treeB.Validate();

        Tree.MultithreadedIntertreeTest<OverlapHandler> context = new(pool);
        OverlapHandler[] handlers = new OverlapHandler[threadDispatcher.ThreadCount];
        for (int i = 0; i < threadDispatcher.ThreadCount; ++i)
        {
            handlers[i].Pairs = new List<(int a, int b)>();
        }
        context.PrepareJobs(ref treeA, ref treeB, handlers, threadDispatcher.ThreadCount);
        threadDispatcher.DispatchWorkers(context.PairTest, context.JobCount);
        context.CompleteTest();
        List<(int a, int b)> multithreadedResults = new();
        for (int i = 0; i < threadDispatcher.ThreadCount; ++i)
        {
            multithreadedResults.AddRange(handlers[i].Pairs);
        }
        SortPairs(multithreadedResults);

        if (singleThreadedResults.Pairs.Count != multithreadedResults.Count)
        {
            throw new Exception("Single threaded vs multithreaded counts don't match.");
        }
        for (int i = 0; i < singleThreadedResults.Pairs.Count; ++i)
        {
            (int a, int b) singleThreadedPair = singleThreadedResults.Pairs[i];
            (int a, int b) multithreadedPair = multithreadedResults[i];
            if (singleThreadedPair.a != multithreadedPair.a ||
                singleThreadedPair.b != multithreadedPair.b)
            {
                throw new Exception("Single threaded vs multithreaded results don't match.");
            }
        }

        //Single and multithreaded variants produce the same results. But do they match a brute force test?
        Tree smaller, larger;
        if (treeA.LeafCount < treeB.LeafCount)
        {
            smaller = treeA;
            larger = treeB;
        }
        else
        {
            smaller = treeB;
            larger = treeA;
        }
        BruteForceResultsEnumerator bruteResultsEnumerator = new();
        bruteResultsEnumerator.Pairs = new List<(int a, int b)>();
        for (int i = 0; i < smaller.LeafCount; ++i)
        {
            GetBoundsForLeaf(smaller, i, out BoundingBox bounds);
            bruteResultsEnumerator.QuerySourceIndex = i;
            larger.GetOverlaps(bounds, pool, ref bruteResultsEnumerator);
        }
        SortPairs(bruteResultsEnumerator.Pairs);

        if (singleThreadedResults.Pairs.Count != bruteResultsEnumerator.Pairs.Count)
        {
            throw new Exception("Brute force vs intertree counts don't match.");
        }
        for (int i = 0; i < singleThreadedResults.Pairs.Count; ++i)
        {
            (int a, int b) singleThreadedPair = singleThreadedResults.Pairs[i];
            (int a, int b) bruteForcePair = bruteResultsEnumerator.Pairs[i];
            if (singleThreadedPair.a != bruteForcePair.a ||
                singleThreadedPair.b != bruteForcePair.b)
            {
                throw new Exception("Brute force vs intertree results don't match.");
            }
        }

        treeA.Dispose(pool);
        treeB.Dispose(pool);
    }

    struct BruteForceResultsEnumerator : IBreakableForEach<int>
    {
        public List<(int a, int b)> Pairs;
        public int QuerySourceIndex;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool LoopBody(int foundIndex)
        {
            Pairs.Add((QuerySourceIndex, foundIndex));
            return true;
        }
    }

    public static void Test()
    {
        Random random = new(5);
        BufferPool pool = new();
        ThreadDispatcher threadDispatcher = new(Environment.ProcessorCount);
        for (int i = 0; i < 1000; ++i)
        {
            TestTrees(pool, threadDispatcher, random);
        }
        pool.Clear();
        threadDispatcher.Dispose();
    }
}
