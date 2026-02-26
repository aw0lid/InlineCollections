# Memory Model

This document explains the memory semantics and lifetime rules for InlineCollections.

## Stack vs heap allocation

### InlineCollections are stack-allocated

When you declare an `InlineList32<int>` as a local variable:

```csharp
void MyMethod()
{
    var list = new InlineList32<int>();  // Allocated on stack
    list.Add(1);
}
// list goes out of scope; stack pointer restored
```

The struct is allocated directly on the method's stack frame. No heap allocation occurs.

**Memory layout on stack**:
```
Stack pointer: [...] [InlineList32<int> = 128 bytes] [← ESP]
              [buffer (32 ints = 128 bytes) + _count (4 bytes)]
```

After the method returns, the stack pointer moves past the struct; memory is implicitly freed.

### Standard collections are heap-allocated

In contrast, `List<T>` is a reference type:

```csharp
void MyMethod()
{
    var list = new List<int>();  // Reference on stack, array on heap
    list.Add(1);                  // Allocates 16 elements on heap
}
// Reference goes out of scope; GC may eventually collect heap array
```

The reference (`this` pointer) sits on the stack, but the data lives on the GC heap.

## Value-type semantics

### Copying on assignment

`InlineList32<T>` is a value type; assignments copy the entire struct:

```csharp
var list1 = new InlineList32<int>();
list1.Add(42);

var list2 = list1;  // COPY: all 32 elements + metadata copied
list2.Add(99);

// list1.Count == 1, list2.Count == 2  (independent!)
```

This is identical to how `int` or `DateTime` work: assignment creates a copy.

### Struct size and copy cost

The copy cost is proportional to struct size. For `InlineList32<int>`:

```
Size = 32 * 4 (elements) + 4 (_count) = 132 bytes
Copy cost ≈ 2-3 nanoseconds (on modern CPUs with cache loaded)
```

For large element types, copy cost increases:

```csharp
struct LargeElement {
    public long A, B, C, D;  // 32 bytes
}

var list = new InlineList32<LargeElement>();
// Size = 32 * 32 + 4 = 1,028 bytes
// Copy cost ≈ 100+ nanoseconds (noticeable!)
```

**Recommendation**: Avoid passing `InlineList32<T>` by value in hot loops. Use `ref` parameter:

```csharp
void Process(ref InlineList32<int> list)  // No copy
{
    list.Add(1);
}

var list = new InlineList32<int>();
Process(ref list);  // Efficient: no copy
```

## Ref struct lifetime safety

### Ref struct constraint

`InlineList32<T>` is a `ref struct`:

```csharp
public ref struct InlineList32<T> where T : unmanaged, IEquatable<T>
{
    private InlineArray32<T> _buffer;
    private int _count;
}
```

This prevents:
- **Storage in reference types**: Cannot be a field of a class
- **Boxing**: Cannot be assigned to `object`
- **Arrays**: Cannot be an array element
- **Async capture**: Cannot be captured in async/await

These restrictions enforce **compile-time safety**: the struct's stack memory is never escaped to the heap or async context.

### Valid vs invalid code

**✅ Valid**:
```csharp
var list = new InlineList32<int>();  // Stack local
list.Add(42);

void Method(ref InlineList32<int> list) { ... }
Method(ref list);  // Pass by ref (reference to stack memory)
```

**❌ Invalid** (compile error):
```csharp
class Container
{
    public InlineList32<int> List;  // ❌ Cannot be a field of a class
}

object boxed = new InlineList32<int>();  // ❌ Cannot box
```

## Inline storage and buffer

### Inline array

The `InlineArray32<T>` struct embeds 32 consecutive elements:

```csharp
[InlineArray(32)]
internal struct InlineArray32<T>
{
    private T _element0;
}
```

At runtime, this becomes a contiguous buffer of 32 `T` values.

**Memory layout for `InlineList32<byte>`**:
```
Offset  Content
------  -------
0-31    32 bytes (elements)
32-35   4 bytes (_count)
```

### Memory access

Methods use `System.Runtime.CompilerServices.Unsafe` for direct memory access:

```csharp
public void Add(T item)
{
    if ((uint)_count >= Capacity) ThrowFull();
    Unsafe.Add(ref _buffer[0], _count++) = item;
}
```

`Unsafe.Add(ref _buffer[0], _count++)` computes the address of the element at index `_count` by pointer arithmetic: no bounds check, no exception.

### Span conversion

`AsSpan()` returns a `Span<T>` over the active portion:

```csharp
public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in _buffer[0]), _count);
```

This creates a span of length `_count` over the buffer, allowing safe iteration and LINQ.

## Stack pressure and struct size

### Stack frame size

Each `InlineList32<int>` occupies ~132 bytes on the stack. Multiple collections add up:

```csharp
void DeepRecursion()
{
    var list1 = new InlineList32<int>();    // 132 bytes
    var list2 = new InlineList32<int>();    // 132 bytes
    var list3 = new InlineList32<int>();    // 132 bytes
    // Frame uses 396 bytes so far

    var large = new InlineList32<LargeStruct>();  // 1KB
    // Frame now 1.4KB

    if (depth < max)
        DeepRecursion();  // Nested frame adds more stack
}
```

Typical stack is 1 MB, so even 1000 frames of 1.4KB each is feasible. However, deeply nested code with many large structs can risk stack overflow.

**Guidance**: Profile stack usage in deep recursion scenarios. Consider using `List<T>` if stack pressure is a concern.


## Enumerator lifetimes

### Span-based enumerator

Enumerators are value types wrapping a `Span<T>`:

```csharp
public ref struct Enumerator
{
    private Span<T> _span;
    private int _index;

    public bool MoveNext() => ++_index < _span.Length;
    public readonly ref T Current => ref _span[_index];
}
```

The enumerator's lifetime is tied to the underlying collection. Modifying the collection during enumeration causes undefined behavior:

```csharp
var list = new InlineList32<int>();
list.Add(1); list.Add(2);

foreach (var item in list)
{
    list.Add(3);  // ❌ Modifying during enumeration (undefined behavior)
}
```

### Safe pattern: copy first

If you must modify during iteration, iterate over a copy:

```csharp
var list = new InlineList32<int>();
list.Add(1); list.Add(2);

var snapshot = list.AsSpan().ToArray();  // Copy to array
foreach (var item in snapshot)
{
    list.Add(3);  // ✅ Safe: iterating over copy
}
```

## Reference returns and in-place modification

### Ref semantics

Indexer and Peek/Peek methods return `ref T`:

```csharp
public ref T this[int index] => ref Unsafe.Add(ref Unsafe.AsRef(in _buffer[0]), index);

public ref T Peek() => ref Unsafe.Add(ref Unsafe.AsRef(in _buffer[0]), _count - 1);
```

This enables in-place modification without copying:

```csharp
ref int top = ref stack.Peek();
top = 100;  // Modifies element directly in stack

ref int elem = ref list[5];
elem += 1;  // Modifies element directly in list
```

### Safety

Ref returns are valid as long as the collection (struct) is not reassigned or moved:

```csharp
var list = new InlineList32<int>();
list.Add(42);

ref int value = ref list[0];
value = 100;  // ✅ Safe: list is in scope

void UseRef(InlineList32<int>& list)
{
    ref int value = ref list[0];  // ❌ Dangerous: ref escapes method scope
}
```

The compiler prevents this pattern. Ref returns must not outlive their source struct.
