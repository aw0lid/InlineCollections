using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace InlineCollections
{
    /// <summary>
    /// Provides a high-performance, stack-allocated List with a fixed capacity of 32 elements.
    /// Targeted at ultra-low latency scenarios with zero heap allocations.
    /// </summary>
    /// <typeparam name="T">The type of unmanaged elements in the list.</typeparam>
    [SkipLocalsInit]
    [StructLayout(LayoutKind.Sequential)]
    public ref struct InlineList32<T> where T : unmanaged, IEquatable<T>
    {
        private InlineArray32<T> _buffer;
        private int _count;

        /// <summary>
        /// The fixed capacity of the <see cref="InlineList32{T}"/>.
        /// </summary>
        public const int Capacity = 32;

        /// <summary>
        /// Gets the number of elements contained in the <see cref="InlineList32{T}"/>.
        /// </summary>
        public readonly int Count => _count;

        /// <summary>
        /// Initializes a new instance of the <see cref="InlineList32{T}"/> struct with initial items.
        /// </summary>
        /// <param name="items">The span of items to initialize the list with.</param>
        public InlineList32(ReadOnlySpan<T> items)
        {
            if (items.Length > Capacity) ThrowFull();
            _buffer = default;
            _count = 0;
            AddRange(items);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InlineList32{T}"/> struct.
        /// </summary>
        public InlineList32()
        {
            _buffer = default;
            _count = 0;
        }

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// High-performance: No bounds checking.
        /// </summary>
        /// <param name="index">The zero-based index of the element.</param>
        public readonly ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref Unsafe.Add(ref Unsafe.AsRef(in _buffer[0]), index);
        }

        /// <summary>
        /// Adds an object to the end of the <see cref="InlineList32{T}"/>.
        /// High-performance: No bounds checking.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            if ((uint)_count >= Capacity) ThrowFull();
            Unsafe.Add(ref _buffer[0], _count++) = item;
        }

        /// <summary>
        /// Tries to add an object to the end of the <see cref="InlineList32{T}"/>.
        /// </summary>
        /// <param name="item">The object to add to the list.</param>
        /// <returns>True if the item was added; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(T item)
        {
            if ((uint)_count >= Capacity) return false;
            Unsafe.Add(ref _buffer[0], _count++) = item;
            return true;
        }

        /// <summary>
        /// Adds the elements of the specified span to the end of the <see cref="InlineList32{T}"/>.
        /// </summary>
        public void AddRange(ReadOnlySpan<T> items)
        {
            if ((uint)(_count + items.Length) > Capacity) ThrowFull();
            items.CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.Add(ref _buffer[0], _count), items.Length));
            _count += items.Length;
        }

        /// <summary>
        /// Inserts an element into the <see cref="InlineList32{T}"/> at the specified index.
        /// High-performance: No bounds checking.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Insert(int index, T item)
        {
            // Direct memory shift - assumes index is valid and count < Capacity
            int remaining = _count - index;
            if (remaining > 0)
            {
                Span<T> baseSpan = MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in _buffer[0]), Capacity);
                baseSpan.Slice(index, remaining).CopyTo(baseSpan.Slice(index + 1));
            }

            Unsafe.Add(ref _buffer[0], index) = item;
            _count++;
        }

        /// <summary>
        /// Tries to insert an element into the <see cref="InlineList32{T}"/> at the specified index.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryInsert(int index, T item)
        {
            if ((uint)_count >= Capacity || (uint)index > (uint)_count)
                return false;

            Insert(index, item);
            return true;
        }

        /// <summary>
        /// Removes the element at the specified index and shifts the remaining elements.
        /// </summary>
        public void RemoveAt(int index)
        {
            if ((uint)index >= (uint)_count) ThrowIndexOutOfRange();
            int remaining = _count - index - 1;
            if (remaining > 0)
            {
                // Optimized memory shift using Spans
                Span<T> baseSpan = MemoryMarshal.CreateSpan(ref _buffer[0], Capacity);
                baseSpan.Slice(index + 1, remaining).CopyTo(baseSpan.Slice(index));
            }
            _count--;
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="InlineList32{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(T item)
        {
            int index = AsSpan().IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether an element is in the <see cref="InlineList32{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Contains(T item) => AsSpan().Contains(item);

        /// <summary>
        /// Removes all objects from the <see cref="InlineList32{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => _count = 0;

        /// <summary>
        /// Returns a span that represents the current elements in the list.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in _buffer[0]), _count);

        /// <summary>
        /// Returns an enumerator for the <see cref="InlineList32{T}"/>.
        /// </summary>
        public Enumerator GetEnumerator() => new(AsSpan());

        /// <summary>
        /// Enumerates the elements of an <see cref="InlineList32{T}"/>.
        /// </summary>
        public ref struct Enumerator
        {
            private Span<T> _span;
            private int _index;

            internal Enumerator(Span<T> span)
            {
                _span = span;
                _index = -1;
            }

            public readonly ref T Current => ref _span[_index];
            public bool MoveNext() => ++_index < _span.Length;
        }

        [DoesNotReturn]
        private static void ThrowFull() => throw new InvalidOperationException("InlineList capacity exceeded (32).");

        [DoesNotReturn]
        private static void ThrowIndexOutOfRange() => throw new ArgumentOutOfRangeException("index", "Index was out of range.");
    }
}