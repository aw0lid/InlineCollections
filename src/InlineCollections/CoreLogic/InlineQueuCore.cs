using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace InlineCollections.CoreLogic
{
    internal static class InlineQueueCore
    {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Enqueue<T>(in T item, Span<T> buffer, ref int tail, ref int count) where T : unmanaged
        {
            if ((uint)count >= (uint)buffer.Length) ThrowFull(buffer.Length);

            Unsafe.Add(ref MemoryMarshal.GetReference(buffer), tail) = item;
            tail = (tail + 1) & (buffer.Length - 1);
            count++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryEnqueue<T>(in T item, Span<T> buffer, ref int tail, ref int count) where T : unmanaged
        {
            if ((uint)count >= (uint)buffer.Length) return false;

            Unsafe.Add(ref MemoryMarshal.GetReference(buffer), tail) = item;
            tail = (tail + 1) & (buffer.Length - 1);
            count++;
            return true;
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T Dequeue<T>(Span<T> buffer, ref int head, ref int count) where T : unmanaged
        {
            if ((uint)count == 0) ThrowEmpty();

            T item = Unsafe.Add(ref MemoryMarshal.GetReference(buffer), head);
            head = (head + 1) & (buffer.Length - 1);
            count--;
            return item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryDequeue<T>(Span<T> buffer, ref int head, ref int count, out T result) where T : unmanaged
        {
            if ((uint)count == 0)
            {
                result = default;
                return false;
            }

            result = Unsafe.Add(ref MemoryMarshal.GetReference(buffer), head);
            head = (head + 1) & (buffer.Length - 1);
            count--;
            return true;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ref T Peek<T>(Span<T> buffer, int head, int count) where T : unmanaged
        {
            if ((uint)count == 0) ThrowEmpty();
            return ref Unsafe.Add(ref MemoryMarshal.GetReference(buffer), head);
        }


        [DoesNotReturn]
        internal static void ThrowFull(int capacity) =>
            throw new InvalidOperationException($"InlineQueue capacity exceeded ({capacity}).");

        [DoesNotReturn]
        private static void ThrowEmpty() =>
            throw new InvalidOperationException("Queue is empty.");
    }
}