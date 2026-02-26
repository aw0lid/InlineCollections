# InlineStack32<T>

A high-performance, stack-allocated LIFO (Last-In-First-Out) collection with a fixed capacity of 32 unmanaged elements.

## Overview

`InlineStack32<T>` provides stack semantics with zero heap allocations. All 32 elements are stored inline within the struct. Push and Pop operations are O(1) and aggressively inlined for minimal call overhead.

**Key characteristics**:
- Fixed capacity: exactly 32 elements
- Stack-allocated: no heap allocation
- Ref struct: cannot be stored in classes or arrays
- LIFO ordering: enumerator iterates in reverse (last pushed first)
- Unsafe push/pop: no bounds checking

## Type signature

```csharp
public ref struct InlineStack32<T> where T : unmanaged, IEquatable<T>
{
    public const int Capacity = 32;
    public int Count { get; }
    // ... methods ...
}
```

## API reference

### Construction

```csharp
// Default constructor: empty stack
var stack = new InlineStack32<int>();

// No span-based constructor; use repeated Push instead
for (int i = 0; i < 5; i++) {
    stack.Push(i);
}
```

### Push and Pop

```csharp
stack.Push(42);           // O(1); throws InvalidOperationException if full

bool success = stack.TryPush(42);  // O(1); returns false if full

int value = stack.Pop();           // O(1); throws InvalidOperationException if empty

bool success = stack.TryPop(out int value);  // O(1); returns false if empty
```

### Peek

```csharp
ref int top = ref stack.Peek();   // O(1); returns ref to top; throws if empty
top = 100;                        // Modify in-place

int value = stack.Peek();         // ❌ Note: Peek() returns ref, not value
                                  // Use TryPop and push back if you need the value
```

### Clearing and querying

```csharp
int count = stack.Count;          // Current element count (0-32)

stack.Clear();                    // O(1); set count to 0

var span = stack.AsSpan();        // O(1); get span over all elements (in insertion order)
```

### Iteration

```csharp
// foreach: enumerator iterates in reverse (LIFO)
var stack = new InlineStack32<int>();
stack.Push(1);
stack.Push(2);
stack.Push(3);

foreach (var item in stack) {
    Console.WriteLine(item);  // Prints: 3, 2, 1 (reverse order)
}

// Manual enumeration
var enumerator = stack.GetEnumerator();
while (enumerator.MoveNext()) {
    ref int current = ref enumerator.Current;  // ref return
    Console.WriteLine(current);
}

// Span iteration (insertion order, not LIFO)
var span = stack.AsSpan();
for (int i = 0; i < span.Length; i++) {
    Console.WriteLine(span[i]);  // Prints: 1, 2, 3 (insertion order)
}
```

## Memory layout

```
Offset  Size     Field
------  --------  -----
0       32*sizeof(T)  _buffer (InlineArray32<T>)
32*sizeof(T)      4   _count
```

**Example for `InlineStack32<int>`** (sizeof(int) = 4):
```
Size = 32*4 + 4 = 132 bytes
Stack frame: [128 bytes of data] [4 bytes count]
```

## Complexity analysis

| Operation | Time | Space |
|-----------|------|-------|
| Push | O(1) | O(1) |
| TryPush | O(1) | O(1) |
| Pop | O(1) | O(1) |
| TryPop | O(1) | O(1) |
| Peek | O(1) | O(1) |
| Clear | O(1) | O(1) |
| AsSpan | O(1) | O(1) |

## Usage examples

### Basic push and pop

```csharp
var stack = new InlineStack32<int>();
stack.Push(10);
stack.Push(20);
stack.Push(30);

Console.WriteLine(stack.Count);  // 3

int top = stack.Pop();  // 30
Console.WriteLine(top);  // 30

int next = stack.Pop();  // 20
Console.WriteLine(next);  // 20
```

### Safe operations with Try- variants

```csharp
var stack = new InlineStack32<int>();
stack.Push(42);

if (stack.TryPop(out int value)) {
    Console.WriteLine(value);  // 42
}

// Stack is now empty
if (stack.TryPop(out _)) {
    Console.WriteLine("Popped");
} else {
    Console.WriteLine("Stack empty");  // This prints
}
```

### In-place modification via Peek

```csharp
var stack = new InlineStack32<double>();
stack.Push(1.5);
stack.Push(2.5);

ref double top = ref stack.Peek();
top *= 2.0;  // Modify top element without popping

Console.WriteLine(stack.Pop());  // 5.0
```

### LIFO iteration

```csharp
var stack = new InlineStack32<string>();
stack.Push("first");
stack.Push("second");
stack.Push("third");

foreach (var item in stack) {
    Console.WriteLine(item);
}
// Output:
// third
// second
// first
```

### Depth-first processing with ref parameter

```csharp
void ProcessStack(ref InlineStack32<Node> stack) {
    // Use ref to avoid struct copy
    while (stack.Count > 0) {
        Node node = stack.Pop();
        Console.WriteLine(node.Value);
        
        if (node.Left != null) stack.Push(node.Left);
        if (node.Right != null) stack.Push(node.Right);
    }
}

var stack = new InlineStack32<Node>();
stack.Push(root);
ProcessStack(ref stack);  // No copy
```

### Stress test: repeated push/pop

```csharp
var stack = new InlineStack32<int>();
for (int i = 0; i < 1000; i++) {
    stack.Push(i);
    Debug.Assert(stack.Count == 1);
    int popped = stack.Pop();
    Debug.Assert(popped == i);
    Debug.Assert(stack.Count == 0);
}
```

### Custom struct elements

```csharp
struct Point : IEquatable<Point> {
    public int X, Y;
    public bool Equals(Point other) => X == other.X && Y == other.Y;
}

var stack = new InlineStack32<Point>();
stack.Push(new Point { X = 1, Y = 2 });
stack.Push(new Point { X = 10, Y = 20 });

var pt = stack.Pop();  // (10, 20)
Console.WriteLine($"{pt.X}, {pt.Y}");
```

## Exceptions

| Exception | Condition |
|-----------|-----------|
| `InvalidOperationException` | Push when Count == 32 |
| `InvalidOperationException` | Pop when Count == 0 |
| `InvalidOperationException` | Peek when Count == 0 |

## Limitations

- **Fixed capacity**: Exactly 32 elements; exceeding throws
- **Unmanaged types only**: `T : unmanaged, IEquatable<T>`
- **No bounds checking on push/pop**: Unsafe methods assume callers verify state (or use Try- variants)
- **Stack storage**: Cannot be stored in reference types or async contexts
- **Value semantics**: Assignment and parameter passing copy the entire struct (up to 132 bytes)

## Enumerator behavior

The enumerator is a `ref struct` that iterates in **reverse order** (LIFO):

```csharp
var stack = new InlineStack32<int>();
stack.Push(1);
stack.Push(2);
stack.Push(3);

// foreach iterates: 3, 2, 1
foreach (var item in stack) {
    Console.WriteLine(item);  // 3, 2, 1
}
```

`AsSpan()` returns elements in insertion order (not LIFO):

```csharp
var span = stack.AsSpan();
// span[0] = 1, span[1] = 2, span[2] = 3
```

## Performance expectations

- **Push/Pop**: Near-identical to stack frame allocation (single instruction)
- **Indexer access**: No indexer; use AsSpan() if indexed access needed
- **Memory**: 0 allocations vs 1 for Stack<T>
- **Copy cost**: ~3ns for `InlineStack32<int>`

## When to use

- Depth-first traversal (trees, graphs)
- Expression parsing (operator stack)
- Undo/redo stacks in UI
- Function call stack simulation
- Hot-path stack operations with fixed max depth

## When NOT to use

- Collections exceeding 32 elements
- Thread-safe stack (use ConcurrentStack<T>)
- Storing in class fields or async contexts
- Unbounded depth applications
