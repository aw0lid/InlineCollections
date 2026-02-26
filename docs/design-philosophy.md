# Design Philosophy

InlineCollections is engineered for ultra-low-latency scenarios where allocation and GC pressure are critical constraints. The library is **not** a general-purpose replacement for BCL collections; rather, it targets a narrow, well-defined set of use cases.

## Core principles

### 1. Zero allocations on the hot path

The primary goal is to eliminate heap allocations for small, short-lived collections. By storing 32 elements inline, we avoid invoking the allocator and GC for the common case.

### 2. Predictability

- APIs are explicit about memory costs: No hidden allocations or copying
- Fixed capacity makes memory footprint calculable
- Exceptions thrown clearly (not silent failures or silent degradation)

### 3. Unsafe where necessary

We use `System.Runtime.CompilerServices.Unsafe` only where it provides measurable performance gain:
- Indexer access without bounds checking
- Direct ref returns for in-place modification
- Aggressive inlining to eliminate call overhead

### 4. Value-type semantics

Collections are `ref struct` types, not reference types. This enforces:
- Stack allocation by default
- No heap indirection
- Compile-time safety (cannot escape to heap or async contexts)

### 5. Simplicity over universality

Fixed capacity of 32 elements (no dynamic growth) keeps the API small and predictable:
- Clear error conditions (throws when full)
- No reallocation or amortization logic
- Straightforward memory layout

## What InlineCollections is

- Optimized for hot paths in high-performance systems
- Zero-allocation primitive for small collections
- Reference-type performance with stack allocation semantics
- Deterministic and cache-friendly

## What InlineCollections is NOT

- A replacement for `List<T>`, `Stack<T>`, or `Queue<T>`
- A thread-safe or concurrent collection
- A general-purpose data structure for arbitrary workloads
- A garbage-collected or reference-type collection

## Design constraints

### Unmanaged elements only

Generic constraint: `T : unmanaged, IEquatable<T>`

This ensures:
- Elements can be blitted (memcpy) without GC concerns
- No references to managed objects
- Safe for stack allocation

### Fixed capacity

Fixed at 32 elements. Why 32?
- 32 * 8 bytes (int64) = 256 bytes; fits in typical cache line hardware prefetch
- Reasonable limit for stack allocation (typical stack frames are kilobytes)
- 32 is a power of 2, enabling fast circular queue operations (`Mask = 31`)

### Ref struct lifetime safety

Being a `ref struct`:
- Compiler prevents storage in reference types (classes, interfaces)
- Prevents boxing
- Prevents async capture
- Ensures memory is not escaped to heap

This is a safety mechanism to prevent dangling references to stack memory.

## Trade-offs

### Inline vs heap

**Inline (InlineCollections)**:
- ✅ No allocation
- ✅ Cache-friendly
- ✅ Fast small operations
- ❌ Fixed capacity
- ❌ Struct copying cost
- ❌ Stack size cost

**Heap (List<T>)**:
- ✅ Dynamic capacity
- ✅ Unbounded growth
- ✅ No copying (reference type)
- ❌ Allocation overhead
- ❌ GC pressure
- ❌ Cache misses

### Unsafe optimization

**Bounds-checked (List<T>)**:
- ✅ Safe by default
- ❌ Branch misprediction overhead

**Unchecked (InlineCollections)**:
- ✅ 0 branches in hot loops
- ✅ Faster indexing
- ❌ Caller must ensure valid indices (in practice, usually guaranteed)

## API design

### Try- variants

Methods like `TryAdd()`, `TryPop()`, `TryDequeue()` allow safe bounds checking without exceptions. They return `bool` and leave collection state unchanged on failure.

### Ref returns

Indexer and Peek/Peek methods return `ref T`, enabling:
```csharp
ref int value = ref list[0];
value = 100;  // Modify in-place, no copy
```

### AsSpan()

Expose memory as a `Span<T>` for LINQ, iteration, and algorithms compatible with the span ecosystem.

## Rationale for fixed capacity

Rather than hybrid (inline + fallback to heap), we chose pure fixed capacity because:

1. **Simplicity**: No state machine for fallback logic
2. **Predictability**: Memory footprint is known at compile time
3. **Safety**: Clear error semantics (throw when full)
4. **Performance**: No branches for capacity checks in hot path
5. **Targeted**: Perfect for scenarios where 32 elements is sufficient

If you need more, use `List<T>`. If you need fewer, you can profile and potentially optimize further.
