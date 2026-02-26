# InlineQueue32<T>

A high-performance, stack-allocated FIFO (First-In-First-Out) collection with a fixed capacity of 32 unmanaged elements using circular buffer semantics.

## Overview

`InlineQueue32<T>` provides queue semantics with zero heap allocations. Internally, it maintains a circular buffer with head and tail pointers, enabling O(1) enqueue and dequeue operations without element shifting.

**Key characteristics**:
- Fixed capacity: exactly 32 elements
- Stack-allocated: no heap allocation
- Circular buffer: O(1) enqueue/dequeue (no shifting)
- Ref struct: cannot be stored in classes or arrays
- Wrap-around handling: mask-based indexing (32 is power of 2)

## Type signature

```csharp
public ref struct InlineQueue32<T> where T : unmanaged, IEquatable<T>
{
    public const int Capacity = 32;
    public int Count { get; }
    // ... methods ...
}
```

## API reference

### Construction

```csharp
// Default constructor: empty queue
var queue = new InlineQueue32<int>();

// No span-based constructor; use repeated Enqueue instead
for (int i = 0; i < 5; i++) {
    queue.Enqueue(i);
}
```

### Enqueue and Dequeue

```csharp
queue.Enqueue(42);           // O(1); throws InvalidOperationException if full

bool success = queue.TryEnqueue(42);  // O(1); returns false if full

int value = queue.Dequeue();           // O(1); throws InvalidOperationException if empty

bool success = queue.TryDequeue(out int value);  // O(1); returns false if empty
```

### Peek

```csharp
ref int front = ref queue.Peek();   // O(1); returns ref to front; throws if empty
front = 100;                        // Modify in-place

// Note: Peek returns ref, not a copy
```

### Clearing and querying

```csharp
int count = queue.Count;          // Current element count (0-32)

queue.Clear();                    // O(1); reset head, tail, count
```

### Iteration

```csharp
// foreach: enumerator iterates in FIFO order (accounting for wrap-around)
var queue = new InlineQueue32<int>();
queue.Enqueue(1);
queue.Enqueue(2);
queue.Enqueue(3);

foreach (var item in queue) {
    Console.WriteLine(item);  // Prints: 1, 2, 3 (FIFO order)
}

// Manual enumeration
var enumerator = queue.GetEnumerator();
while (enumerator.MoveNext()) {
    ref readonly int current = ref enumerator.Current;  // ref readonly
    Console.WriteLine(current);
}
```

## Internal design: Circular buffer

### How it works

A circular buffer uses modulo arithmetic to reuse space:

```
State: head=0, tail=0, count=0 (empty)

After Enqueue(1), Enqueue(2), Enqueue(3):
  head=0, tail=3, count=3
  Buffer: [1, 2, 3, _, _, ...]

After Dequeue(), Dequeue():
  head=2, tail=3, count=1
  Buffer: [1, 2, 3, _, _, ...]  (head moved, tail fixed)

After Enqueue(4), Enqueue(5):
  head=2, tail=1, count=3  (wrap: tail = (3+2) & 31 = 5 & 31 = 5)
  Actually: head=2, tail=5, count=3
  Buffer: [5, 2, 3, 4, _, ...]  (overwrites from position 3)
```

### Mask-based wrap-around

Since capacity is 32 (power of 2), wrap-around uses bitwise AND:

```csharp
_tail = (_tail + 1) & Mask;  // Where Mask = 31
_head = (_head + 1) & Mask;
```

This is faster than modulo (`%`): one instruction vs division.

## Memory layout

```
Offset  Size     Field
------  --------  -----
0       32*sizeof(T)  _buffer (InlineArray32<T>)
32*sizeof(T)      4   _head (circular index)
32*sizeof(T)+4    4   _tail (circular index)
32*sizeof(T)+8    4   _count
```

**Example for `InlineQueue32<int>`** (sizeof(int) = 4):
```
Size = 32*4 + 4 + 4 + 4 = 140 bytes
```

## Complexity analysis

| Operation | Time | Space |
|-----------|------|-------|
| Enqueue | O(1) | O(1) |
| TryEnqueue | O(1) | O(1) |
| Dequeue | O(1) | O(1) |
| TryDequeue | O(1) | O(1) |
| Peek | O(1) | O(1) |
| Clear | O(1) | O(1) |

## Usage examples

### Basic enqueue and dequeue

```csharp
var queue = new InlineQueue32<int>();
queue.Enqueue(10);
queue.Enqueue(20);
queue.Enqueue(30);

Console.WriteLine(queue.Count);  // 3

int front = queue.Dequeue();  // 10
Console.WriteLine(front);      // 10

int next = queue.Dequeue();  // 20
Console.WriteLine(next);     // 20
```

### Safe operations with Try- variants

```csharp
var queue = new InlineQueue32<int>();
queue.Enqueue(42);

if (queue.TryDequeue(out int value)) {
    Console.WriteLine(value);  // 42
}

// Queue is now empty
if (queue.TryDequeue(out _)) {
    Console.WriteLine("Dequeued");
} else {
    Console.WriteLine("Queue empty");  // This prints
}
```

### Wrap-around behavior

The queue reuses buffer space as elements are dequeued:

```csharp
var queue = new InlineQueue32<int>();

// Fill to 32 (full)
for (int i = 0; i < 32; i++) {
    queue.Enqueue(i);
}
Console.WriteLine(queue.Count);  // 32

// Remove first 16
for (int i = 0; i < 16; i++) {
    queue.Dequeue();
}
Console.WriteLine(queue.Count);  // 16

// Add 16 more (wraps around in the buffer)
for (int i = 0; i < 16; i++) {
    queue.Enqueue(i + 100);
}
Console.WriteLine(queue.Count);  // 32 (full again)

// Dequeue all; should come out in order
for (int i = 0; i < 16; i++) {
    Console.WriteLine(queue.Dequeue());  // 16-31
}
for (int i = 0; i < 16; i++) {
    Console.WriteLine(queue.Dequeue());  // 100-115
}
```

### In-place modification via Peek

```csharp
var queue = new InlineQueue32<double>();
queue.Enqueue(1.5);
queue.Enqueue(2.5);

ref double front = ref queue.Peek();
front *= 2.0;  // Modify front element without dequeuing

Console.WriteLine(queue.Dequeue());  // 3.0
```

### FIFO iteration

```csharp
var queue = new InlineQueue32<string>();
queue.Enqueue("first");
queue.Enqueue("second");
queue.Enqueue("third");

foreach (var item in queue) {
    Console.WriteLine(item);
}
// Output:
// first
// second
// third
```

### Bounded buffer: packet processing

```csharp
void ProcessPackets(ref InlineQueue32<Packet> queue) {
    // Use ref to avoid struct copy
    while (queue.Count > 0) {
        Packet pkt = queue.Dequeue();
        ProcessPacket(ref pkt);
    }
}

var queue = new InlineQueue32<Packet>();
// ... populate ...
ProcessPackets(ref queue);
```

### Stress test: continuous enqueue/dequeue

```csharp
var queue = new InlineQueue32<int>();
for (int i = 0; i < 1000; i++) {
    queue.Enqueue(i);
    Debug.Assert(queue.Count == 1);
    int dequeued = queue.Dequeue();
    Debug.Assert(dequeued == i);
    Debug.Assert(queue.Count == 0);
}
```

### Cyclic fill and drain

```csharp
var queue = new InlineQueue32<int>();

// Fill completely
for (int cycle = 0; cycle < 10; cycle++) {
    for (int i = 0; i < 32; i++) {
        queue.Enqueue(cycle * 1000 + i);
    }
    
    // Drain completely
    for (int i = 0; i < 32; i++) {
        int val = queue.Dequeue();
        Console.WriteLine(val);
    }
}
```

## Exceptions

| Exception | Condition |
|-----------|-----------|
| `InvalidOperationException` | Enqueue when Count == 32 |
| `InvalidOperationException` | Dequeue when Count == 0 |
| `InvalidOperationException` | Peek when Count == 0 |

## Limitations

- **Fixed capacity**: Exactly 32 elements; exceeding throws
- **Unmanaged types only**: `T : unmanaged, IEquatable<T>`
- **No bounds checking**: Unsafe methods assume valid state (or use Try- variants)
- **Stack storage**: Cannot be stored in reference types or async contexts
- **Value semantics**: Assignment and parameter passing copy the entire struct (up to 140 bytes)

## Enumerator behavior

The enumerator is a `ref struct` that handles wrap-around correctly:

```csharp
var queue = new InlineQueue32<int>();
for (int i = 1; i <= 20; i++) queue.Enqueue(i);
for (int i = 0; i < 10; i++) queue.Dequeue();  // Dequeue 1-10
for (int i = 21; i <= 30; i++) queue.Enqueue(i);  // Enqueue 21-30

// foreach correctly handles wrap-around
foreach (var item in queue) {
    Console.WriteLine(item);  // Prints: 11-20, 21-30 (correct FIFO order)
}
```

## Performance expectations

- **Enqueue/Dequeue**: O(1) constant time, minimal instructions
- **No shifting**: Unlike RemoveAt on lists, queue operations don't move elements
- **Memory**: 0 allocations vs 1 for Queue<T>
- **Copy cost**: ~4ns for `InlineQueue32<int>` (slightly larger due to 3 fields vs 2 in stack)

## When to use

- Message queues with bounded depth
- Packet buffers in networking
- Task schedulers with fixed work queue
- BFS (breadth-first search) with bounded depth
- Frame-local processing queues

## When NOT to use

- Queues exceeding 32 elements
- Thread-safe queues (use ConcurrentQueue<T>)
- Storing in class fields or async contexts
- Unbounded or highly variable queue depths
