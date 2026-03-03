using System.Runtime.CompilerServices;


namespace InlineCollections.Enumeration
{
    public ref struct Enumerator<T> where T : unmanaged, IEquatable<T>
    {
        private readonly Span<T> _span;
        private int _index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(Span<T> span)
        {
            _span = span;
            _index = -1;
        }

        public readonly ref T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _span[_index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            int next = _index + 1;
            if ((uint)next < (uint)_span.Length)
            {
                _index = next;
                return true;
            }
            return false;
        }
    }



    public ref struct StackEnumerator<T> where T : unmanaged
    {
        private readonly ReadOnlySpan<T> _buffer;
        private int _index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal StackEnumerator(ReadOnlySpan<T> buffer, int count)
        {
            _buffer = buffer;
            _index = count;
        }

        public readonly ref readonly T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _buffer[_index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            return --_index >= 0;
        }
    }



    public ref struct QueueEnumerator<T> where T : unmanaged
    {
        private readonly ReadOnlySpan<T> _buffer;
        private readonly int _head;
        private readonly int _count;
        private readonly int _mask;
        private int _index;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal QueueEnumerator(ReadOnlySpan<T> buffer, int head, int count, int mask)
        {
            _buffer = buffer;
            _head = head;
            _count = count;
            _mask = mask;
            _index = -1;
        }

        public readonly ref readonly T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _buffer[(_head + _index) & _mask];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            return ++_index < _count;
        }
    }
}