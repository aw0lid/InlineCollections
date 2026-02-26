using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace InlineCollections
{
    /// <summary>
    /// Provides a high-performance, stack-allocated Stack with a fixed capacity of 32 elements.
    /// Eliminates heap allocations and reduces GC pressure.
    /// </summary>
    /// <typeparam name="T">The type of unmanaged elements in the stack.</typeparam>
    [SkipLocalsInit]
    [StructLayout(LayoutKind.Sequential)]
    public ref struct InlineStack32<T> where T : unmanaged, IEquatable<T>
    {
        private InlineArray32<T> _buffer;
        private int _count;

        /// <summary>
        /// The fixed capacity of the <see cref="InlineStack32{T}"/>.
        /// </summary>
        public const int Capacity = 32;

        /// <summary>
        /// Gets the number of elements contained in the <see cref="InlineStack32{T}"/>.
        /// </summary>
        public readonly int Count => _count;

        /// <summary>
        /// Initializes a new instance of the <see cref="InlineStack32{T}"/> struct.
        /// </summary>
        public InlineStack32()
        {
            _buffer = default;
            _count = 0;
        }

        /// <summary>
        /// Inserts an object at the top of the <see cref="InlineStack32{T}"/>.
        /// </summary>
        /// <param name="item">The object to push onto the stack.</param>
        /// <exception cref="InvalidOperationException">Thrown when the stack is full.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(T item)
        {
            if ((uint)_count >= Capacity) ThrowFull();
            // Direct memory access for peak performance
            Unsafe.Add(ref _buffer[0], _count++) = item;
        }

        /// <summary>
        /// Removes and returns the object at the top of the <see cref="InlineStack32{T}"/>.
        /// </summary>
        /// <returns>The object removed from the top of the stack.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the stack is empty.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Pop()
        {
            if ((uint)_count == 0) ThrowEmpty();
            return Unsafe.Add(ref _buffer[0], --_count);
        }

        /// <summary>
        /// Returns the object at the top of the <see cref="InlineStack32{T}"/> without removing it.
        /// </summary>
        /// <returns>A reference to the object at the top of the stack.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the stack is empty.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref T Peek()
        {
            if (_count == 0) ThrowEmpty();
            return ref Unsafe.Add(ref Unsafe.AsRef(in _buffer[0]), _count - 1);
        }

        /// <summary>
        /// Tries to push an item onto the stack without throwing an exception.
        /// </summary>
        /// <returns>True if the item was pushed; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPush(T item)
        {
            if ((uint)_count >= Capacity) return false;
            Unsafe.Add(ref _buffer[0], _count++) = item;
            return true;
        }

        /// <summary>
        /// Tries to pop an item from the stack without throwing an exception.
        /// </summary>
        /// <param name="result">The popped item if successful.</param>
        /// <returns>True if an item was popped; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPop(out T result)
        {
            if (_count == 0)
            {
                result = default;
                return false;
            }
            result = Unsafe.Add(ref _buffer[0], --_count);
            return true;
        }

        /// <summary>
        /// Removes all objects from the <see cref="InlineStack32{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => _count = 0;

        /// <summary>
        /// Returns a span that represents the current elements in the stack.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in _buffer[0]), _count);

        /// <summary>
        /// Returns an enumerator for the <see cref="InlineStack32{T}"/>.
        /// </summary>
        public Enumerator GetEnumerator() => new(AsSpan());

        /// <summary>
        /// Enumerates the elements of an <see cref="InlineStack32{T}"/>.
        /// </summary>
        public ref struct Enumerator
        {
            private readonly Span<T> _span;
            private int _index;

            internal Enumerator(Span<T> span)
            {
                _span = span;
                _index = span.Length;
            }

            public readonly ref T Current => ref _span[_index];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => --_index >= 0;
        }

        [DoesNotReturn]
        private static void ThrowFull() => throw new InvalidOperationException("Stack Full");

        [DoesNotReturn]
        private static void ThrowEmpty() => throw new InvalidOperationException("Stack Empty");
    }
}