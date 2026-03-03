# InlineList<T> (sizes 8, 16, 32)

A high-performance, stack-allocated list with fixed capacity of 8, 16, or 32 unmanaged elements.

## Overview

`InlineList8<T>`, `InlineList16<T>`, and `InlineList32<T>` provide List-like semantics with zero heap allocations. Elements are stored inline within the struct. Choose your size based on typical working set:
- **InlineList8**: Minimal overhead (~36 bytes), 8-element max
- **InlineList16**: Moderate overhead (~68 bytes), 16-element max
- **InlineList32**: Larger overhead (~132 bytes), 32-element max

> **Caution:** The struct's stack footprint scales with `Capacity * sizeof(T)`.
> Using large element types can lead to significant stack pressure and even
> `StackOverflowException`. In performance-critical code, pass the list by
> `ref`/`in` or opt for a smaller capacity or heap-based collection.

All sizes are `ref struct` types optimized for ultra-low latency.

**Key characteristics**:
- Fixed capacity: exactly 8, 16, or 32 elements (depending on variant)
- Stack-allocated: no heap allocation
- Ref struct: cannot be stored in classes or arrays
- Zero-copy Span: efficient iteration and algorithms
- Unsafe indexing: no bounds checking for performance

## Type signature

```csharp
public ref struct InlineList8<T> where T : unmanaged, IEquatable<T>
{
    public const int Capacity = 8;
    public int Count { get; }
    // ... methods ...
}

// Similarly for InlineList16<T> and InlineList32<T>
// with Capacity = 16 and 32 respectively
```

## API reference

### Construction

```csharp
// Default constructor: empty list
var list8 = new InlineList8<int>();
var list16 = new InlineList16<int>();
var list32 = new InlineList32<int>();

// Constructor with initial span: copies elements (respects capacity)
ReadOnlySpan<int> items = stackalloc int[] { 1, 2, 3 };
var list = new InlineList8<int>(items);  // Count becomes 3
```

Throws `InvalidOperationException` if the span length exceeds the capacity (8, 16, or 32).

### Adding elements

```csharp
list.Add(42);  // O(1); throws InvalidOperationException if full

bool success = list.TryAdd(42);  // O(1); returns false if full, does not throw

list.AddRange(new int[] { 1, 2, 3 });  // O(n); throws if result exceeds capacity
```

### Indexing and access

```csharp
int value = list[0];              // O(1); ref return (no bounds check)
list[0] = 99;                     // Modify in-place

ref int elem = ref list[5];       // Get ref for in-place modification
elem += 1;

var span = list.AsSpan();         // O(1); span over active elements
foreach (var item in span) { ... }
```

### Insertion and removal

```csharp
list.Insert(2, 99);        // O(n); insert at index, shift right; throws if full

bool success = list.TryInsert(2, 99);  // O(n); returns false if invalid index or full

list.RemoveAt(2);          // O(n); shift left; throws if index invalid

bool found = list.Remove(42);  // O(n); removes first occurrence, returns false if not found

list.Clear();              // O(1); set count to 0
```

### Querying

```csharp
int count = list.Count;         // Current element count (0-32)

bool has42 = list.Contains(42);  // O(n); linear search

var span = list.AsSpan();       // O(1); get span over elements
```

### Iteration

```csharp
// foreach: uses ref struct enumerator
foreach (var item in list) {
    Console.WriteLine(item);  // Iterates in order (0 to Count-1)
}

// Manual enumeration
var enumerator = list.GetEnumerator();
while (enumerator.MoveNext()) {
    ref int current = ref enumerator.Current;  // ref return
    Console.WriteLine(current);
}

// Span iteration (preferred for performance)
var span = list.AsSpan();
for (int i = 0; i < span.Length; i++) {
    Console.WriteLine(span[i]);
}
```

## Memory layout

```
Offset  Size     Field
------  --------  -----
0       32*sizeof(T)  _buffer (InlineArray32<T>)
32*sizeof(T)      4   _count
```

**Example for `InlineList32<int>`** (sizeof(int) = 4):
```
Size = 32*4 + 4 = 132 bytes
Stack frame: [128 bytes of data] [4 bytes count]
```

## Complexity analysis

| Operation | Time | Space |
|-----------|------|-------|
| Add | O(1) | O(1) |
| TryAdd | O(1) | O(1) |
| AddRange(n) | O(n) | O(1) |
| Insert | O(n) | O(1) |
| Remove | O(n) | O(1) |
| RemoveAt | O(n) | O(1) |
| Contains | O(n) | O(1) |
| Clear | O(1) | O(1) |
| Indexer get/set | O(1) | O(1) |
| AsSpan | O(1) | O(1) |

## Usage examples

### Basic operations

```csharp
var list = new InlineList32<int>();
list.Add(10);
list.Add(20);
list.Add(30);

Console.WriteLine(list.Count);  // 3
Console.WriteLine(list[1]);      // 20

list.RemoveAt(1);
Console.WriteLine(list[1]);      // 30
```

### Bulk operations

```csharp
var list = new InlineList32<int>();
Span<int> data = stackalloc int[] { 1, 2, 3, 4, 5 };
list.AddRange(data);  // Add all 5 elements

Console.WriteLine(list.Count);  // 5

var span = list.AsSpan();
var sum = span.Sum();  // Use Span<T> with LINQ
Console.WriteLine(sum);  // 15
```

### In-place modification

```csharp
var list = new InlineList32<double>();
list.Add(1.5);
list.Add(2.5);

ref double elem = ref list[0];
elem *= 2.0;  // Modify in-place, no copy

Console.WriteLine(list[0]);  // 3.0
```

### Iteration patterns

```csharp
var list = new InlineList32<string>();
list.Add("foo");
list.Add("bar");
list.Add("baz");

// foreach
foreach (var item in list) {
    Console.WriteLine(item);
}

// Span iteration (preferred for perf)
var span = list.AsSpan();
foreach (var item in span) {
    Console.WriteLine(item);
}
```

### Performance-critical code

```csharp
void ProcessBatch(ref InlineList32<Packet> packets) {
    // Use ref to avoid struct copy
    var span = packets.AsSpan();
    for (int i = 0; i < span.Length; i++) {
        ref Packet pkt = ref span[i];
        pkt.ProcessAndMark();  // In-place modification
    }
}

var packets = new InlineList32<Packet>();
// ... populate ...
ProcessBatch(ref packets);  // No copy
```

## Exceptions

| Exception | Condition |
|-----------|-----------|
| `InvalidOperationException` | Add/Insert when Count == capacity (8, 16, or 32) |
| `InvalidOperationException` | AddRange when result would exceed capacity |
| `ArgumentOutOfRangeException` | RemoveAt with invalid index |

## Limitations

- **Fixed capacity**: Exactly 8, 16, or 32 elements (depending on variant); exceeding throws
- **Unmanaged types only**: `T : unmanaged, IEquatable<T>`
- **Unsafe indexing**: No bounds checking on indexer; caller must ensure valid indices
- **Stack storage**: Cannot be stored in reference types or async contexts
- **Value semantics**: Assignment and parameter passing copy the entire struct (36-132 bytes depending on size)
- **Stack usage**: Choose appropriate size; `InlineList32<int>` uses ~132 bytes, `InlineList8<int>` uses ~36 bytes

## Performance expectations

- **Small element types** (`int`, `long`): 3-7x faster than `List<T>` for Add operations (zero allocation)
- **Indexer access**: Near-identical to `List<T>` (both use direct memory access)
- **Memory**: 0 allocations vs 1 for `List<T>`
- **Copy cost**: 
  - `InlineList8<int>`: ~1ns
  - `InlineList16<int>`: ~2ns
  - `InlineList32<int>`: ~3ns

## When to use

- Hot-path loops creating many small lists
- Fixed-size buffers or working sets (8-32 elements)
- Tight inner loops where allocation is a bottleneck
- Stack-based processing (frame-local state)
- Network packet buffers or protocol parsing
- Choose size based on typical capacity needs (8 for minimal, 32 for maximum)

## When NOT to use

- Collections exceeding 32 elements
- Scenarios with dynamic, unbounded growth
- Storing in class fields or async contexts
- When List<T> API compatibility is required
- Reference-type or nullable elements
