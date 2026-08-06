using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace BepuUtilities.Collections
{
    /// <summary>
    /// Provides optimized equality testing, comparison, and hashing for primitive types.
    /// </summary>
    /// <typeparam name="T">Type to compare and hash.</typeparam>
    public struct PrimitiveComparer<T> : IEqualityComparerRef<T>, IComparerRef<T>
    {
        //using T4 templates? pfah

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(ref T a, ref T b)
        {
            if (typeof(T) == typeof(bool) || typeof(T) == typeof(byte))
            {
                byte aTemp = Unsafe.As<T, byte>(ref a);
                byte bTemp = Unsafe.As<T, byte>(ref b);
                return aTemp > bTemp ? 1 : aTemp < bTemp ? -1 : 0;
            }
            if (typeof(T) == typeof(sbyte))
            {
                sbyte aTemp = Unsafe.As<T, sbyte>(ref a);
                sbyte bTemp = Unsafe.As<T, sbyte>(ref b);
                return aTemp > bTemp ? 1 : aTemp < bTemp ? -1 : 0;
            }
            if (typeof(T) == typeof(short))
            {
                short aTemp = Unsafe.As<T, short>(ref a);
                short bTemp = Unsafe.As<T, short>(ref b);
                return aTemp > bTemp ? 1 : aTemp < bTemp ? -1 : 0;
            }
            if (typeof(T) == typeof(ushort))
            {
                ushort aTemp = Unsafe.As<T, ushort>(ref a);
                ushort bTemp = Unsafe.As<T, ushort>(ref b);
                return aTemp > bTemp ? 1 : aTemp < bTemp ? -1 : 0;
            }
            if (typeof(T) == typeof(int))
            {
                int aTemp = Unsafe.As<T, int>(ref a);
                int bTemp = Unsafe.As<T, int>(ref b);
                return aTemp > bTemp ? 1 : aTemp < bTemp ? -1 : 0;
            }
            if (typeof(T) == typeof(uint))
            {
                uint aTemp = Unsafe.As<T, uint>(ref a);
                uint bTemp = Unsafe.As<T, uint>(ref b);
                return aTemp > bTemp ? 1 : aTemp < bTemp ? -1 : 0;
            }
            if (typeof(T) == typeof(long))
            {
                long aTemp = Unsafe.As<T, long>(ref a);
                long bTemp = Unsafe.As<T, long>(ref b);
                return aTemp > bTemp ? 1 : aTemp < bTemp ? -1 : 0;
            }
            if (typeof(T) == typeof(ulong))
            {
                ulong aTemp = Unsafe.As<T, ulong>(ref a);
                ulong bTemp = Unsafe.As<T, ulong>(ref b);
                return aTemp > bTemp ? 1 : aTemp < bTemp ? -1 : 0;
            }
            if (typeof(T) == typeof(IntPtr))
            {
                unsafe
                {
                    void* aTemp = Unsafe.As<T, IntPtr>(ref a).ToPointer();
                    void* bTemp = Unsafe.As<T, IntPtr>(ref b).ToPointer();
                    return aTemp < bTemp ? -1 : aTemp > bTemp ? -1 : 0;
                }
            }
            if (typeof(T) == typeof(UIntPtr))
            {
                unsafe
                {
                    void* aTemp = Unsafe.As<T, UIntPtr>(ref a).ToPointer();
                    void* bTemp = Unsafe.As<T, UIntPtr>(ref b).ToPointer();
                    return aTemp < bTemp ? -1 : aTemp > bTemp ? -1 : 0;
                }
            }
            if (typeof(T) == typeof(char))
            {
                char aTemp = Unsafe.As<T, char>(ref a);
                char bTemp = Unsafe.As<T, char>(ref b);
                return aTemp > bTemp ? 1 : aTemp < bTemp ? -1 : 0;
            }
            if (typeof(T) == typeof(double))
            {
                double aTemp = Unsafe.As<T, double>(ref a);
                double bTemp = Unsafe.As<T, double>(ref b);
                return aTemp > bTemp ? 1 : aTemp < bTemp ? -1 : 0;
            }
            if (typeof(T) == typeof(float))
            {
                float aTemp = Unsafe.As<T, float>(ref a);
                float bTemp = Unsafe.As<T, float>(ref b);
                return aTemp > bTemp ? 1 : aTemp < bTemp ? -1 : 0;
            }
            Debug.Assert(false, "Should only use the supported primitive types with the primitive comparer.");
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ref T a, ref T b)
        {
            if (typeof(T) == typeof(bool))
            {
                return Unsafe.As<T, bool>(ref a) == Unsafe.As<T, bool>(ref b);
            }
            if (typeof(T) == typeof(byte))
            {
                return Unsafe.As<T, byte>(ref a) == Unsafe.As<T, byte>(ref b);
            }
            if (typeof(T) == typeof(sbyte))
            {
                return Unsafe.As<T, sbyte>(ref a) == Unsafe.As<T, sbyte>(ref b);
            }
            if (typeof(T) == typeof(short))
            {
                return Unsafe.As<T, short>(ref a) == Unsafe.As<T, short>(ref b);
            }
            if (typeof(T) == typeof(ushort))
            {
                return Unsafe.As<T, ushort>(ref a) == Unsafe.As<T, ushort>(ref b);
            }
            if (typeof(T) == typeof(int))
            {
                return Unsafe.As<T, int>(ref a) == Unsafe.As<T, int>(ref b);
            }
            if (typeof(T) == typeof(uint))
            {
                return Unsafe.As<T, uint>(ref a) == Unsafe.As<T, uint>(ref b);
            }
            if (typeof(T) == typeof(long))
            {
                return Unsafe.As<T, long>(ref a) == Unsafe.As<T, long>(ref b);
            }
            if (typeof(T) == typeof(ulong))
            {
                return Unsafe.As<T, ulong>(ref a) == Unsafe.As<T, ulong>(ref b);
            }
            if (typeof(T) == typeof(IntPtr))
            {
                return Unsafe.As<T, IntPtr>(ref a) == Unsafe.As<T, IntPtr>(ref b);
            }
            if (typeof(T) == typeof(UIntPtr))
            {
                return Unsafe.As<T, UIntPtr>(ref a) == Unsafe.As<T, UIntPtr>(ref b);
            }
            if (typeof(T) == typeof(char))
            {
                return Unsafe.As<T, char>(ref a) == Unsafe.As<T, char>(ref b);
            }
            if (typeof(T) == typeof(double))
            {
                return Unsafe.As<T, double>(ref a) == Unsafe.As<T, double>(ref b);
            }
            if (typeof(T) == typeof(float))
            {
                return Unsafe.As<T, float>(ref a) == Unsafe.As<T, float>(ref b);
            }
            Debug.Assert(false, "Should only use the supported primitive types with the primitive comparer.");
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Hash(ref T item)
        {
            //Note: the jit is able to inline the GetHashCodes; no need for custom implementations.
            if (typeof(T) == typeof(bool))
            {
                return Unsafe.As<T, bool>(ref item).GetHashCode();
            }
            if (typeof(T) == typeof(byte))
            {
                return Unsafe.As<T, byte>(ref item).GetHashCode();
            }
            if (typeof(T) == typeof(sbyte))
            {
                return Unsafe.As<T, sbyte>(ref item).GetHashCode();
            }
            if (typeof(T) == typeof(short))
            {
                return Unsafe.As<T, short>(ref item).GetHashCode();
            }
            if (typeof(T) == typeof(ushort))
            {
                return Unsafe.As<T, ushort>(ref item).GetHashCode();
            }
            if (typeof(T) == typeof(int))
            {
                return Unsafe.As<T, int>(ref item).GetHashCode();
            }
            if (typeof(T) == typeof(uint))
            {
                return Unsafe.As<T, uint>(ref item).GetHashCode();
            }
            if (typeof(T) == typeof(long))
            {
                return Unsafe.As<T, long>(ref item).GetHashCode();
            }
            if (typeof(T) == typeof(ulong))
            {
                return Unsafe.As<T, ulong>(ref item).GetHashCode();
            }
            if (typeof(T) == typeof(IntPtr))
            {
                return Unsafe.As<T, IntPtr>(ref item).GetHashCode();
            }
            if (typeof(T) == typeof(UIntPtr))
            {
                return Unsafe.As<T, UIntPtr>(ref item).GetHashCode();
            }
            if (typeof(T) == typeof(char))
            {
                return Unsafe.As<T, char>(ref item).GetHashCode();
            }
            if (typeof(T) == typeof(double))
            {
                return Unsafe.As<T, double>(ref item).GetHashCode();
            }
            if (typeof(T) == typeof(float))
            {
                return Unsafe.As<T, float>(ref item).GetHashCode();
            }
            Debug.Assert(false, "Should only use the supported primitive types with the primitive comparer.");
            return 0;
        }
    }
}
