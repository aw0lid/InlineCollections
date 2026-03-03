using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using InlineCollections.CoreLogic;
using InlineCollections.Enumeration;

namespace InlineCollections
{
    /// <summary>
    /// Provides a high-performance, stack-allocated Queue with a fixed capacity of 32 elements.
    /// Eliminates heap allocations and reduces GC pressure.
    /// </summary>
    /// <remarks>
    /// Since elements are stored inline, the struct size becomes <c>Capacity * sizeof(T)</c>.
    /// Large unmanaged element types can impose significant stack pressure and risk a
    /// <see cref="StackOverflowException"/>. To reduce copying cost, pass the queue by
    /// <c>ref</c> or <c>in</c>, or choose a smaller variant or heap-allocated alternative
    /// for large element sizes.
    /// </remarks>
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
        /// Returns an enumerator for the <see cref="InlineQueue32{T}"/>.
        /// </summary>
        public QueueEnumerator<T> GetEnumerator() => new(AsSpan(), _head, _count, Mask);

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
            InlineQueueCore.Enqueue(in item, _buffer, ref _tail, ref _count);
        }

        /// <summary>
        /// Tries to add an object to the end of the <see cref="InlineQueue32{T}"/>.
        /// </summary>
        /// <param name="item">The object to add to the queue.</param>
        /// <returns>True if the item was added; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEnqueue(T item)
        {
            return InlineQueueCore.TryEnqueue(in item, _buffer, ref _tail, ref _count);
        }

        /// <summary>
        /// Removes and returns the object at the beginning of the <see cref="InlineQueue32{T}"/>.
        /// </summary>
        /// <returns>The object removed from the beginning of the queue.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the queue is empty.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Dequeue()
        {
            return InlineQueueCore.Dequeue<T>(_buffer, ref _head, ref _count);
        }

        /// <summary>
        /// Tries to remove and return the object at the beginning of the <see cref="InlineQueue32{T}"/>.
        /// </summary>
        /// <param name="result">The object removed from the beginning of the queue if successful.</param>
        /// <returns>True if an item was dequeued; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDequeue([MaybeNullWhen(false)] out T result)
        {
            return InlineQueueCore.TryDequeue<T>(_buffer, ref _head, ref _count, out result);
        }

        /// <summary>
        /// Returns the object at the beginning of the <see cref="InlineQueue32{T}"/> without removing it.
        /// </summary>
        /// <returns>A reference to the object at the beginning of the queue.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the queue is empty.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref T Peek()
        {
            return ref InlineQueueCore.Peek<T>(this.AsSpan(), _head, _count);
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
        private readonly Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in _buffer[0]), Capacity);
    }
}