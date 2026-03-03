using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace InlineCollections.CoreLogic
{
    internal static class InlineListCore
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Add<T>(in T item, Span<T> buffer, ref int count) where T : unmanaged
        {
            if ((uint)count >= (uint)buffer.Length) ThrowFull(buffer.Length);
            Unsafe.Add(ref MemoryMarshal.GetReference(buffer), count++) = item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryAdd<T>(in T item, Span<T> buffer, ref int count) where T : unmanaged
        {
            if ((uint)count >= (uint)buffer.Length) return false;
            Unsafe.Add(ref MemoryMarshal.GetReference(buffer), count++) = item;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AddRange<T>(ReadOnlySpan<T> items, Span<T> buffer, ref int count) where T : unmanaged
        {
            if ((uint)(count + items.Length) > (uint)buffer.Length) ThrowFull(buffer.Length);
            items.CopyTo(buffer.Slice(count));
            count += items.Length;
        }




        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Insert<T>(int index, in T item, Span<T> buffer, ref int count) where T : unmanaged
        {
            if ((uint)index > (uint)count) ThrowIndexOutOfRange();
            if ((uint)count >= (uint)buffer.Length) ThrowFull(buffer.Length);

            int remaining = count - index;
            if (remaining > 0)
            {
                buffer.Slice(index, remaining).CopyTo(buffer.Slice(index + 1));
            }

            Unsafe.Add(ref MemoryMarshal.GetReference(buffer), index) = item;
            count++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryInsert<T>(int index, in T item, Span<T> buffer, ref int count) where T : unmanaged
        {
            if ((uint)count >= (uint)buffer.Length || (uint)index > (uint)count)
                return false;

            Insert(index, in item, buffer, ref count);
            return true;
        }




        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RemoveAt<T>(int index, Span<T> buffer, ref int count) where T : unmanaged
        {
            if ((uint)index >= (uint)count) ThrowIndexOutOfRange();

            int remaining = count - index - 1;
            if (remaining > 0)
            {
                buffer.Slice(index + 1, remaining).CopyTo(buffer.Slice(index));
            }
            count--;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool Remove<T>(in T item, Span<T> buffer, ref int count) where T : unmanaged, IEquatable<T>
        {
            int index = IndexOf(in item, buffer, count);
            if (index < 0) return false;

            RemoveAt(index, buffer, ref count);
            return true;
        }




        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOf<T>(in T item, ReadOnlySpan<T> buffer, int count) where T : unmanaged, IEquatable<T>
        {
            return buffer.Slice(0, count).IndexOf(item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool Contains<T>(in T item, ReadOnlySpan<T> buffer, int count) where T : unmanaged, IEquatable<T>
        {
            return IndexOf(in item, buffer, count) >= 0;
        }




        [DoesNotReturn]
        internal static void ThrowFull(int capacity) =>
            throw new InvalidOperationException($"InlineList capacity exceeded ({capacity}).");

        [DoesNotReturn]
        private static void ThrowIndexOutOfRange() =>
            throw new ArgumentOutOfRangeException("index", "Index was out of range.");
    }
}