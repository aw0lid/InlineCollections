# InlineList32<T>

A high-performance, stack-allocated list with a fixed capacity of 32 unmanaged elements.

## Overview

`InlineList32<T>` provides List-like semantics with zero heap allocations for the fast path. All 32 elements are stored inline within the struct, eliminating allocation and GC overhead for small, short-lived lists.

**Key characteristics**:
- Fixed capacity: exactly 32 elements
- Stack-allocated: no heap allocation
- Ref struct: cannot be stored in classes or arrays
- Zero-copy Span: efficient iteration and algorithms
- Unsafe indexing: no bounds checking for performance

## Type signature

```csharp
public ref struct InlineList32<T> where T : unmanaged, IEquatable<T>
{
    public const int Capacity = 32;
    public int Count { get; }
    // ... methods ...
}
```

## API reference

### Construction

```csharp
// Default constructor: empty list
var list = new InlineList32<int>();

// Constructor with initial span: copies elements
ReadOnlySpan<int> items = stackalloc int[] { 1, 2, 3 };
var list = new InlineList32<int>(items);  // Count becomes 3
```

Throws `InvalidOperationException` if the span length exceeds capacity (32).

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
| `InvalidOperationException` | Add/Insert when Count == 32 |
| `InvalidOperationException` | AddRange when result would exceed 32 |
| `ArgumentOutOfRangeException` | RemoveAt with invalid index |

## Limitations

- **Fixed capacity**: Exactly 32 elements; exceeding throws
- **Unmanaged types only**: `T : unmanaged, IEquatable<T>`
- **Unsafe indexing**: No bounds checking on indexer; caller must ensure valid indices
- **Stack storage**: Cannot be stored in reference types or async contexts
- **Value semantics**: Assignment and parameter passing copy the entire struct (up to 132 bytes)

## Performance expectations

- **Small element types** (`int`, `long`): 3-5x faster than `List<T>` for Add operations (zero allocation)
- **Indexer access**: Near-identical to `List<T>` (both use direct memory access)
- **Memory**: 0 allocations vs 1 for `List<T>`
- **Copy cost**: ~3ns for `InlineList32<int>`, scales with element size

## When to use

- Hot-path loops creating many small lists
- Fixed-size buffers or working sets up to 32 elements
- Tight inner loops where allocation is a bottleneck
- Stack-based processing (frame-local state)
- Network packet buffers or protocol parsing

## When NOT to use

- Collections exceeding 32 elements
- Scenarios with dynamic, unbounded growth
- Storing in class fields or async contexts
- When List<T> API compatibility is required
- Reference-type or nullable elements
