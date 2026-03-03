using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using InlineCollections.CoreLogic;
using InlineCollections.Enumeration;

namespace InlineCollections
{
    /// <summary>
    /// Provides a high-performance, stack-allocated Stack with a fixed capacity of 32 elements.
    /// Eliminates heap allocations and reduces GC pressure.
    /// </summary>
    /// <remarks>
    /// Because elements are stored inline, the struct size is <c>Capacity * sizeof(T)</c>.
    /// Using a large unmanaged <c>T</c> can generate significant stack pressure and may
    /// trigger a <see cref="StackOverflowException"/>. Pass by <c>ref</c> or <c>in</c> to
    /// minimize copies, or pick a smaller variant or another collection for large types.
    /// </remarks>
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
        /// Returns an enumerator for the <see cref="InlineStack32{T}"/>.
        /// </summary>
        public StackEnumerator<T> GetEnumerator() => new(AsSpan(), _count);

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
            InlineStackCore.Push(in item, _buffer, ref _count);
        }

        /// <summary>
        /// Removes and returns the object at the top of the <see cref="InlineStack32{T}"/>.
        /// </summary>
        /// <returns>The object removed from the top of the stack.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the stack is empty.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Pop()
        {
            return InlineStackCore.Pop<T>(_buffer, ref _count);
        }

        /// <summary>
        /// Returns the object at the top of the <see cref="InlineStack32{T}"/> without removing it.
        /// </summary>
        /// <returns>A reference to the object at the top of the stack.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the stack is empty.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref T Peek()
        {
            return ref InlineStackCore.Peek<T>(this.AsSpan(), _count);
        }

        /// <summary>
        /// Tries to push an item onto the stack without throwing an exception.
        /// </summary>
        /// <returns>True if the item was pushed; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPush(T item)
        {
            return InlineStackCore.TryPush(in item, _buffer, ref _count);
        }

        /// <summary>
        /// Tries to pop an item from the stack without throwing an exception.
        /// </summary>
        /// <param name="result">The popped item if successful.</param>
        /// <returns>True if an item was popped; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPop(out T result)
        {
            return InlineStackCore.TryPop(_buffer, ref _count, out result);
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
    }
}