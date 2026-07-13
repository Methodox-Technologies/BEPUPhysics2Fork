using BepuUtilities.Collections;
using BepuUtilities.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace BEPUutilitiesTests
{
    public static class QuickCollectionTests
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void TestQueueResizing(IUnmanagedMemoryPool pool)
        {
            Random random = new(5);

            QuickQueue<int> queue = new(4, pool);
            Queue<int> controlQueue = new();

            for (int iterationIndex = 0; iterationIndex < 1000000; ++iterationIndex)
            {
                if (random.NextDouble() < 0.7)
                {
                    queue.Enqueue(iterationIndex, pool);
                    controlQueue.Enqueue(iterationIndex);
                }
                if (random.NextDouble() < 0.2)
                {
                    queue.Dequeue();
                    controlQueue.Dequeue();
                }
                if (iterationIndex % 1000 == 0)
                {
                    queue.EnsureCapacity(queue.Count * 3, pool);
                }
                else if (iterationIndex % 7777 == 0)
                {
                    queue.Compact(pool);
                }
            }

            Debug.Assert(queue.Count == controlQueue.Count, "e");
            while (queue.Count > 0)
            {
                int a = queue.Dequeue();
                int b = controlQueue.Dequeue();
                Debug.Assert(a == b);
                Debug.Assert(queue.Count == controlQueue.Count);
            }

            queue.Dispose(pool);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void TestListResizing(IUnmanagedMemoryPool pool)
        {
            Random random = new(5);
            QuickList<int> list = new(4, pool);
            List<int> controlList = new();

            for (int iterationIndex = 0; iterationIndex < 100000; ++iterationIndex)
            {
                if (random.NextDouble() < 0.7)
                {
                    list.Add(iterationIndex, pool);
                    controlList.Add(iterationIndex);
                }
                if (random.NextDouble() < 0.2)
                {
                    int indexToRemove = random.Next(list.Count);
                    list.RemoveAt(indexToRemove);
                    controlList.RemoveAt(indexToRemove);
                }
                if (iterationIndex % 1000 == 0)
                {
                    list.EnsureCapacity(list.Count * 3, pool);
                }
                else if (iterationIndex % 7777 == 0)
                {
                    list.Compact(pool);
                }
            }

            Debug.Assert(list.Count == controlList.Count);
            for (int i = 0; i < list.Count; ++i)
            {
                int a = list[i];
                int b = controlList[i];
                Debug.Assert(a == b);
                Debug.Assert(list.Count == controlList.Count);
            }

            list.Dispose(pool);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void TestSetResizing(IUnmanagedMemoryPool pool)
        {
            Random random = new(5);
            QuickSet<int, PrimitiveComparer<int>> set = new(4, pool);
            HashSet<int> controlSet = new();

            for (int iterationIndex = 0; iterationIndex < 100000; ++iterationIndex)
            {
                if (random.NextDouble() < 0.7)
                {
                    set.Add(iterationIndex, pool);
                    controlSet.Add(iterationIndex);
                }
                if (random.NextDouble() < 0.2)
                {
                    int indexToRemove = random.Next(set.Count);
                    int toRemove = set[indexToRemove];
                    set.FastRemove(toRemove);
                    controlSet.Remove(toRemove);
                }
                if (iterationIndex % 1000 == 0)
                {
                    set.EnsureCapacity(set.Count * 3, pool);
                }
                else if (iterationIndex % 7777 == 0)
                {
                    set.Compact(pool);
                }
            }

            Debug.Assert(set.Count == controlSet.Count);
            for (int i = 0; i < set.Count; ++i)
            {
                Debug.Assert(controlSet.Contains(set[i]));
            }
            foreach (int element in controlSet)
            {
                Debug.Assert(set.Contains(element));
            }

            set.Dispose(pool);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void TestDictionaryResizing(IUnmanagedMemoryPool pool)
        {
            Random random = new(5);
            QuickDictionary<int, int, PrimitiveComparer<int>> dictionary = new(4, pool);
            Dictionary<int, int> controlDictionary = new();

            for (int iterationIndex = 0; iterationIndex < 100000; ++iterationIndex)
            {
                if (random.NextDouble() < 0.7)
                {
                    dictionary.Add(iterationIndex, iterationIndex, pool);
                    controlDictionary.Add(iterationIndex, iterationIndex);
                }
                if (random.NextDouble() < 0.2)
                {
                    int indexToRemove = random.Next(dictionary.Count);
                    int toRemove = dictionary.Keys[indexToRemove];
                    dictionary.FastRemove(toRemove);
                    controlDictionary.Remove(toRemove);
                }
                if (iterationIndex % 1000 == 0)
                {
                    dictionary.EnsureCapacity(dictionary.Count * 3, pool);
                }
                else if (iterationIndex % 7777 == 0)
                {
                    dictionary.Compact(pool);
                }
            }

            Debug.Assert(dictionary.Count == controlDictionary.Count);
            for (int i = 0; i < dictionary.Count; ++i)
            {
                Debug.Assert(controlDictionary.ContainsKey(dictionary.Keys[i]));
            }
            foreach (int element in controlDictionary.Keys)
            {
                Debug.Assert(dictionary.ContainsKey(element));
            }
            dictionary.Dispose(pool);
        }

        public static void Test()
        {
            BufferPool bufferPool = new(256);
            TestQueueResizing(bufferPool);
            TestListResizing(bufferPool);
            TestSetResizing(bufferPool);
            TestDictionaryResizing(bufferPool);
            bufferPool.Clear();
            

        }
    }
}
