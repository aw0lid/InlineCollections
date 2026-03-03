using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using InlineCollections.CoreLogic;
using InlineCollections.Enumeration;

namespace InlineCollections
{
    /// <summary>
    /// Provides a high-performance, stack-allocated List with a fixed capacity of 16 elements.
    /// Targeted at ultra-low latency scenarios with zero heap allocations.
    /// </summary>
    /// <remarks>
    /// Elements are stored inline, so the struct size equals <c>Capacity * sizeof(T)</c>.
    /// Using a large unmanaged <c>T</c> may impose significant stack memory pressure and
    /// could lead to a <see cref="StackOverflowException"/>. Consider passing the
    /// collection by <c>ref</c> or <c>in</c> to avoid expensive copies, or choose a
    /// smaller variant (8 or 16) or a heap-based collection when storing large types.
    /// </remarks>
    /// <typeparam name="T">The type of unmanaged elements in the list.</typeparam>
    [SkipLocalsInit]
    [StructLayout(LayoutKind.Sequential)]
    public ref struct InlineList16<T> where T : unmanaged, IEquatable<T>
    {
        private InlineArray16<T> _buffer;
        private int _count;

        /// <summary>
        /// The fixed capacity of the <see cref="InlineList16{T}"/>.
        /// </summary>
        public const int Capacity = 16;

        /// <summary>
        /// Gets the number of elements contained in the <see cref="InlineList16{T}"/>.
        /// </summary>
        public readonly int Count => _count;

        /// <summary>
        /// Returns an enumerator for the <see cref="InlineList16{T}"/>.
        /// </summary>
        public Enumerator<T> GetEnumerator() => new(AsSpan());


        /// <summary>
        /// Initializes a new instance of the <see cref="InlineList16{T}"/> struct with initial items.
        /// </summary>
        /// <param name="items">The span of items to initialize the list with.</param>
        public InlineList16(ReadOnlySpan<T> items)
        {
            if (items.Length > Capacity) InlineListCore.ThrowFull(Capacity);
            _buffer = default;
            _count = 0;
            AddRange(items);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InlineList16{T}"/> struct.
        /// </summary>
        public InlineList16()
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
        /// Adds an object to the end of the <see cref="InlineList16{T}"/>.
        /// High-performance: No bounds checking.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            InlineListCore.Add(in item, _buffer, ref _count);
        }

        /// <summary>
        /// Tries to add an object to the end of the <see cref="InlineList16{T}"/>.
        /// </summary>
        /// <param name="item">The object to add to the list.</param>
        /// <returns>True if the item was added; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(T item)
        {
            return InlineListCore.TryAdd(in item, _buffer, ref _count);
        }

        /// <summary>
        /// Adds the elements of the specified span to the end of the <see cref="InlineList16{T}"/>.
        /// </summary>
        public void AddRange(ReadOnlySpan<T> items)
        {
            InlineListCore.AddRange(items, _buffer, ref _count);
        }

        /// <summary>
        /// Inserts an element into the <see cref="InlineList16{T}"/> at the specified index.
        /// High-performance: No bounds checking.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Insert(int index, T item)
        {
            InlineListCore.Insert(index, in item, _buffer, ref _count);
        }

        /// <summary>
        /// Tries to insert an element into the <see cref="InlineList16{T}"/> at the specified index.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryInsert(int index, T item)
        {
            return InlineListCore.TryInsert(index, in item, _buffer, ref _count);
        }

        /// <summary>
        /// Removes the element at the specified index and shifts the remaining elements.
        /// </summary>
        public void RemoveAt(int index)
        {
            InlineListCore.RemoveAt<T>(index, _buffer, ref _count);
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="InlineList16{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(T item)
        {
            return InlineListCore.Remove(in item, _buffer, ref _count);
        }

        /// <summary>
        /// Determines whether an element is in the <see cref="InlineList16{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Contains(T item) => InlineListCore.Contains(in item, _buffer, _count);

        /// <summary>
        /// Removes all objects from the <see cref="InlineList16{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => _count = 0;

        /// <summary>
        /// Returns a span that represents the current elements in the list.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in _buffer[0]), _count);
    }
}