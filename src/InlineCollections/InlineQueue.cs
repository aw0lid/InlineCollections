using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace InlineCollections
{
    /// <summary>
    /// Provides a high-performance, stack-allocated Queue with a fixed capacity of 32 elements.
    /// Eliminates heap allocations and reduces GC pressure.
    /// </summary>
    /// <typeparam name="T">The type of unmanaged elements in the queue.</typeparam>
    [SkipLocalsInit]
    [StructLayout(LayoutKind.Sequential)]
    public ref struct InlineQueue32<T> where T : unmanaged, IEquatable<T>
    {
        private InlineArray32<T> _buffer;
        private int _head;
        private int _tail;
        private int _count;

        /// <summary>
        /// The fixed capacity of the <see cref="InlineQueue32{T}"/>.
        /// </summary>
        public const int Capacity = 32;
        private const int Mask = Capacity - 1;

        /// <summary>
        /// Gets the number of elements contained in the <see cref="InlineQueue32{T}"/>.
        /// </summary>
        public readonly int Count => _count;

        /// <summary>
        /// Initializes a new instance of the <see cref="InlineQueue32{T}"/> struct.
        /// </summary>
        public InlineQueue32()
        {
            _buffer = default;
            _head = 0;
            _tail = 0;
            _count = 0;
        }

        /// <summary>
        /// Adds an object to the end of the <see cref="InlineQueue32{T}"/>.
        /// </summary>
        /// <param name="item">The object to add to the queue.</param>
        /// <exception cref="InvalidOperationException">Thrown when the queue is full.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(T item)
        {
            if ((uint)_count >= Capacity) ThrowFull();
            Unsafe.Add(ref _buffer[0], _tail) = item;
            _tail = (_tail + 1) & Mask;
            _count++;
        }

        /// <summary>
        /// Tries to add an object to the end of the <see cref="InlineQueue32{T}"/>.
        /// </summary>
        /// <param name="item">The object to add to the queue.</param>
        /// <returns>True if the item was added; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEnqueue(T item)
        {
            if ((uint)_count >= Capacity) return false;
            Unsafe.Add(ref _buffer[0], _tail) = item;
            _tail = (_tail + 1) & Mask;
            _count++;
            return true;
        }

        /// <summary>
        /// Removes and returns the object at the beginning of the <see cref="InlineQueue32{T}"/>.
        /// </summary>
        /// <returns>The object removed from the beginning of the queue.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the queue is empty.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Dequeue()
        {
            if (_count == 0) ThrowEmpty();
            T item = Unsafe.Add(ref _buffer[0], _head);
            _head = (_head + 1) & Mask;
            _count--;
            return item;
        }

        /// <summary>
        /// Tries to remove and return the object at the beginning of the <see cref="InlineQueue32{T}"/>.
        /// </summary>
        /// <param name="result">The object removed from the beginning of the queue if successful.</param>
        /// <returns>True if an item was dequeued; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDequeue([MaybeNullWhen(false)] out T result)
        {
            if (_count == 0)
            {
                result = default;
                return false;
            }
            result = Unsafe.Add(ref _buffer[0], _head);
            _head = (_head + 1) & Mask;
            _count--;
            return true;
        }

        /// <summary>
        /// Returns the object at the beginning of the <see cref="InlineQueue32{T}"/> without removing it.
        /// </summary>
        /// <returns>A reference to the object at the beginning of the queue.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the queue is empty.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref T Peek()
        {
            if (_count == 0) ThrowEmpty();
            return ref Unsafe.Add(ref Unsafe.AsRef(in _buffer[0]), _head);
        }

        /// <summary>
        /// Removes all objects from the <see cref="InlineQueue32{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _head = 0;
            _tail = 0;
            _count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly ReadOnlySpan<T> AsFullSpanReadOnly() => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in _buffer[0]), Capacity);

        /// <summary>
        /// Returns an enumerator for the <see cref="InlineQueue32{T}"/>.
        /// </summary>
        public Enumerator GetEnumerator() => new Enumerator(AsFullSpanReadOnly(), _head, _count);

        /// <summary>
        /// Enumerates the elements of an <see cref="InlineQueue32{T}"/>.
        /// </summary>
        public ref struct Enumerator
        {
            private readonly ReadOnlySpan<T> _buffer;
            private readonly int _head;
            private readonly int _count;
            private int _index;

            internal Enumerator(ReadOnlySpan<T> buffer, int head, int count)
            {
                _buffer = buffer;
                _head = head;
                _count = count;
                _index = -1;
            }

            public readonly ref readonly T Current => ref _buffer[(_head + _index) & Mask];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                return ++_index < _count;
            }
        }

        [DoesNotReturn] private static void ThrowFull() => throw new InvalidOperationException("Queue Full");
        [DoesNotReturn] private static void ThrowEmpty() => throw new InvalidOperationException("Queue Empty");
    }
}