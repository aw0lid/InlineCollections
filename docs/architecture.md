# Architecture

This document describes the technical design and memory layout of InlineCollections.

## System overview

InlineCollections provides three collection types: `InlineList32<T>`, `InlineStack32<T>`, and `InlineQueue32<T>`. All are `ref struct` types backed by inline storage using the `InlineArray32<T>` helper struct.

---

> [!IMPORTANT]
> **Positioning Statement**: This library is **not a general-purpose replacement** for the standard .NET `System.Collections.Generic` types. Standard collections are designed for flexibility and large datasets. `InlineCollections` are "surgical tools" designed for **High-Performance hot-paths** where the developer has a guaranteed bound on the number of elements (≤ 32) and must eliminate heap allocations to reduce GC pressure and latency.

---

## Core design: InlineArray32<T>

```csharp
[InlineArray(32)]
internal struct InlineArray32<T> {
    private T _element0;
}
```

The `InlineArray32<T>` struct uses the C# 12+ `[InlineArray(32)]` attribute to embed 32 contiguous elements directly in the struct. This means when you create an `InlineList32<int>`, the struct contains 32 integers inline—no separate heap allocation.

## Memory layout

### InlineList32<T>

```
Offset  Size     Field
------  --------  -----
0       32*sz(T)  _buffer (InlineArray32<T>)
32*sz(T) 4        _count (int)
```

- `_buffer`: fixed array of 32 elements
- `_count`: current element count (0 to 32)

**Total size**: `32 * sizeof(T) + 4` bytes, usually padded to alignment boundary.

### InlineStack32<T>

```
Offset  Size     Field
------  --------  -----
0       32*sz(T)  _buffer (InlineArray32<T>)
32*sz(T) 4        _count (int)
```

Identical layout to InlineList; the difference is semantic (LIFO vs indexed).

### InlineQueue32<T>

```
Offset  Size     Field
------  --------  -----
0       32*sz(T)  _buffer (InlineArray32<T>)
32*sz(T) 4        _head (int)
32*sz(T)+4  4    _tail (int)
32*sz(T)+8  4    _count (int)
```

- `_head`: index of the front element (circular)
- `_tail`: index of the next insertion slot (circular)
- `_count`: current element count

The circular buffer uses bitwise AND with `Mask = 31` to wrap indices.

## Allocation model

- **Stack allocation**: Structs are allocated on the call stack (by default for local variables)
- **No heap allocation**: The inline 32-element buffer is part of the struct itself
- **Ref struct semantics**: Cannot be stored in reference types, arrays, or async contexts
- **Value-type copying**: Assignment and parameter passing copy the entire struct

## Unsafe optimizations

InlineCollections uses `System.Runtime.CompilerServices.Unsafe` for performance:

- `Unsafe.Add()` — direct memory access without bounds checking
- `Unsafe.AsRef()` — cast const references to mutable for modification
- `MemoryMarshal.CreateSpan()` — create spans over managed memory
- `SkipLocalsInit` attribute — avoid zero-initialization overhead on struct allocation

Example:
```csharp
[SkipLocalsInit]
public ref struct InlineList32<T> where T : unmanaged, IEquatable<T>
{
    private InlineArray32<T> _buffer;
    private int _count;

    public void Add(T item)
    {
        if ((uint)_count >= Capacity) ThrowFull();
        Unsafe.Add(ref _buffer[0], _count++) = item;
    }
}
```

The indexer returns a `ref T` allowing callers to modify elements in-place:
```csharp
public ref T this[int index]
{
    get => ref Unsafe.Add(ref Unsafe.AsRef(in _buffer[0]), index);
}
```

## Performance characteristics

### Inlining and JIT

- AggressiveInlining attributes on hot methods (Add, Pop, Enqueue, Indexer)
- JIT produces nearly branch-free code for the common path
- Stack allocation is free (just a stack pointer adjustment)

### Cache locality

- All 32 elements stored contiguously in the struct
- No pointer indirection; memory is inline
- Cache line fits ~8-16 elements (depending on element size), reducing cache misses

### Allocation-free fast path

- No calls to `GC.Alloc()`
- No marking as roots or scanning by GC
- Scales with local variable lifetime, not GC heap

## Ref struct constraints

Being a `ref struct`, these collections cannot:
- Be stored as fields in classes or reference types
- Be boxed
- Be used in async methods
- Be passed across `await` boundaries
- Be stored in arrays

This is a safety feature ensuring stack-allocated memory is not referenced from the heap.

## Comparison with standard collections

| Aspect | InlineList32 | List<T> |
|--------|-------------|---------|
| Storage | Inline (stack) | Heap |
| Allocation | 0 | 1 per instance |
| Capacity | Fixed 32 | Dynamic |
| Ref struct | Yes | No |
| Thread-safe | No | No |
| Indexer perf | O(1), no bounds check | O(1), bounds check |
| Max elements | 32 | ~2 billion |

## Design principles

1. **Zero allocations**: Prioritize stack allocation and inline storage
2. **Unsafe by necessity**: Use `Unsafe` only where bounds-checking costs are unacceptable
3. **Ref semantics**: Return refs to allow in-place modification
4. **Fixed capacity**: Simplify API and memory model (no dynamic growth)
5. **Aggressive inlining**: Reduce call overhead in hot loops

## Module responsibilities

- `src/InlineCollections/`: Core collection types
- `src/InlineCollections/InlineArray.cs`: Helper struct for inline storage
- `src/InlineCollections/InlineList.cs`: List implementation
- `src/InlineCollections/InlineStack.cs`: Stack implementation
- `src/InlineCollections/InlineQueue.cs`: Queue implementation
- `tests/`: Unit tests for correctness, boundary conditions, and stress scenarios
- `benchmarks/`: BenchmarkDotNet harness for performance validation
