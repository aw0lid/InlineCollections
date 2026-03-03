using System.Runtime.CompilerServices;

namespace InlineCollections
{

    [InlineArray(8)]
    internal struct InlineArray8<T> where T : unmanaged, IEquatable<T> { private T _element0; }

    [InlineArray(16)]
    internal struct InlineArray16<T> where T : unmanaged, IEquatable<T> { private T _element0; }

    [InlineArray(32)]
    internal struct InlineArray32<T> where T : unmanaged, IEquatable<T> { private T _element0; }
}