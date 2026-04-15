using BepuUtilities.Collections;
using BepuUtilities.Memory;
using System;
using System.Runtime.CompilerServices;

namespace BEPUutilitiesTests
{
    public static class CodeGenTests
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestPrimitiveComparer()
        {
            PrimitiveComparer<int> comparer = default(PrimitiveComparer<int>);
            int a = 2;
            int b = 4;

            bool equal = comparer.Equals(ref a, ref b);
            int hashcode = comparer.Hash(ref a);
            bool isPrimitive = SpanHelper.IsPrimitive<int>();
            if (SpanHelper.IsPrimitive<bool>())
                Console.WriteLine("prim prim");

            Console.WriteLine($"Equality: {equal}, hash: {hashcode}");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestDefaultComparer<T>(T a, T b)
        {
            WrapperEqualityComparer<T>.CreateDefault(out WrapperEqualityComparer<T> comparer);

            bool equal = comparer.Equals(ref a, ref b);
            int hashcode = comparer.Hash(ref a);
            if (SpanHelper.IsPrimitive<T>())
                Console.WriteLine("prim prom");

            Console.WriteLine($"Equality: {equal}, hash: {hashcode}");
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        unsafe static void TestQuickInlining(BufferPool pool)
        {
            {
                QuickSet<double, PrimitiveComparer<double>> set = new(4, pool);
                set.AddUnsafely(5);
                double item = set[0];
                Console.WriteLine($"Managed Item: {item}");

                PrimitiveComparer<double> comparer = default(PrimitiveComparer<double>);
                int hash = comparer.Hash(ref item);

                Console.WriteLine($"Hash: {hash}");
            }

            {

                QuickSet<int, PrimitiveComparer<int>> set = new(4, pool);
                set.AddUnsafely(5);
                int item = set[0];
            }

        }

        public static void Test()
        {
            BufferPool pool = new();
            TestPrimitiveComparer();

            TestDefaultComparer(2, 4);
            TestDefaultComparer(2L, 2L);
            TestDefaultComparer("hey", "sup");
            
            TestQuickInlining(pool);
            pool.Clear();

        }
    }
}

