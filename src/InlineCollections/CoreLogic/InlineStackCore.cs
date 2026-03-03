using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace InlineCollections.CoreLogic
{
    internal static class InlineStackCore
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Push<T>(in T item, Span<T> buffer, ref int count) where T : unmanaged
        {
            if ((uint)count >= (uint)buffer.Length) ThrowFull(buffer.Length);
            Unsafe.Add(ref MemoryMarshal.GetReference(buffer), count++) = item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryPush<T>(in T item, Span<T> buffer, ref int count) where T : unmanaged
        {
            if ((uint)count >= (uint)buffer.Length) return false;
            Unsafe.Add(ref MemoryMarshal.GetReference(buffer), count++) = item;
            return true;
        }




        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T Pop<T>(Span<T> buffer, ref int count) where T : unmanaged
        {
            if ((uint)count == 0) ThrowEmpty();
            return Unsafe.Add(ref MemoryMarshal.GetReference(buffer), --count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryPop<T>(Span<T> buffer, ref int count, out T result) where T : unmanaged
        {
            if ((uint)count == 0)
            {
                result = default;
                return false;
            }
            result = Unsafe.Add(ref MemoryMarshal.GetReference(buffer), --count);
            return true;
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ref T Peek<T>(Span<T> buffer, int count) where T : unmanaged
        {
            if ((uint)count == 0) ThrowEmpty();
            return ref Unsafe.Add(ref MemoryMarshal.GetReference(buffer), count - 1);
        }


        [DoesNotReturn]
        internal static void ThrowFull(int capacity) =>
            throw new InvalidOperationException($"InlineStack capacity exceeded ({capacity}).");

        [DoesNotReturn]
        private static void ThrowEmpty() =>
            throw new InvalidOperationException("Stack is empty.");
    }
}