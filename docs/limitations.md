# Limitations

This section enumerates the hard constraints and limitations of InlineCollections that developers must understand before adoption.

## Fixed capacity

**Hard limit**: Exactly 32 elements. Attempting to exceed throws `InvalidOperationException`.

```csharp
var list = new InlineList32<int>();
for (int i = 0; i < 33; i++) {
    list.Add(i);  // ❌ Throws InvalidOperationException on i=32
}
```

**Impact**:
- Collections cannot grow dynamically
- Capacity planning required upfront
- Unsuitable for unbounded workloads

**Recommendation**: Profile and measure. If you consistently hit or exceed 32 elements, use `List<T>`.

## Unmanaged elements only

**Constraint**: `T : unmanaged, IEquatable<T>`

Unmanaged means:
- No managed object references (no string, no class instances)
- Primitive types: int, long, float, double, byte, etc.
- Unmanaged structs: all fields must be unmanaged

**Invalid examples**:
```csharp
var list = new InlineList32<string>();        // ❌ string is managed
var list = new InlineList32<object>();        // ❌ object is managed
var list = new InlineList32<List<int>>();     // ❌ nested reference type

struct WithRef { string Name; }                // ❌ Contains managed field
var list = new InlineList32<WithRef>();
```

**Valid examples**:
```csharp
var list = new InlineList32<int>();           // ✅ int is unmanaged
var list = new InlineList32<Guid>();          // ✅ Guid is unmanaged struct
var list = new InlineList32<Point>();         // ✅ struct Point { int x, y; }

struct Point {
    public int X, Y;                           // Both unmanaged
}
```

**Why**: Inline storage and stack allocation require GC-safe memory. Managed references must be traced by GC.

## Stack-allocated constraints

**Ref struct rules**:
- Cannot be stored in class fields
- Cannot be boxed
- Cannot be used in async/await
- Cannot be captured in closures that outlive the frame

**Invalid examples**:
```csharp
class Container {
    public InlineList32<int> List;  // ❌ Cannot store in class
}

object boxed = new InlineList32<int>();  // ❌ Cannot box

async Task ProcessAsync() {
    var list = new InlineList32<int>();
    await SomeAsync();  // ❌ list cannot escape to async
}
```

**Valid examples**:
```csharp
var list = new InlineList32<int>();  // ✅ Local variable
void Method(ref InlineList32<int> list) { ... }  // ✅ ref parameter
```

## Value-type copying cost

**Constraint**: Assignments and parameter passing copy the entire struct.

```csharp
var list1 = new InlineList32<int>();
list1.Add(1);

var list2 = list1;  // COPY: 132 bytes for InlineList32<int>
list2.Add(2);

// list1 and list2 are independent
```

**Copy cost**: ~3 nanoseconds for `InlineList32<int>` on modern CPUs, but scales with element type:

| Element type | Struct size | Copy time |
|--------------|-------------|-----------|
| int (4B) | 132B | ~3ns |
| long (8B) | 260B | ~6ns |
| Guid (16B) | 516B | ~12ns |

**Impact**: In tight loops with large element types, copying overhead can negate allocation savings.

**Recommendation**: Use `ref` parameters in hot paths:
```csharp
// ❌ Copies struct
void Process(InlineList32<int> list) { list.Add(99); }

// ✅ No copy
void Process(ref InlineList32<int> list) { list.Add(99); }
```

## Stack pressure

**Constraint**: Each collection on the stack occupies memory. Deep recursion or many collections in a frame can cause stack overflow.

```csharp
void DeepRecursion(int depth) {
    var list = new InlineList32<int>();  // 132 bytes
    var stack = new InlineStack32<int>(); // 132 bytes
    var queue = new InlineQueue32<int>(); // 140 bytes
    // Frame: ~404 bytes

    if (depth < 10000)
        DeepRecursion(depth + 1);  // Each recursive call adds 404 bytes
}
```

Typical stack: 1 MB on .NET
Max depth: ~2500 levels before overflow

**Recommendation**: Profile stack usage. For deep recursion, consider `List<T>` or heap-based structures.

## No bounds checking

**Constraint**: Indexer and unsafe methods (Add, Push, Enqueue) do not bounds check.

```csharp
var list = new InlineList32<int>();
list.Add(42);

int value = list[1];  // ❌ Out of bounds! Undefined behavior
```

**Mitigation**: Use Try- variants:
```csharp
if (list.TryAdd(42)) {
    // Safe
} else {
    // Handle full
}
```

## Element size and struct overhead

**Constraint**: Large element types increase struct size and copy cost.

```csharp
struct BigData { public long[100] Data; }  // Managed array! Invalid.
struct BigStruct {
    public double A, B, C, D, E, F, G, H;  // 64 bytes
}

// InlineList32<BigStruct> is 32*64+4 = 2052 bytes
var list = new InlineList32<BigStruct>();
// Stack frame usage: 2KB just for this collection!
```

**Recommendation**: Prefer small element types (int, long, small structs). If element size > 8 bytes, evaluate whether allocation cost of `List<T>` is acceptable.

## No thread-safety

**Constraint**: Collections are not thread-safe. Concurrent access is undefined behavior.

```csharp
var list = new InlineList32<int>();
Task.Run(() => list.Add(1));
Task.Run(() => list.Add(2));
// ❌ Race condition, possible corruption
```

**Mitigation**: Use locks or concurrent collections:
```csharp
var lockObj = new object();
lock (lockObj) {
    list.Add(1);  // ✅ Protected
}
```

## Exception types and messages

**Exceptions thrown**:

| Exception | Method | Condition |
|-----------|--------|-----------|
| `InvalidOperationException` "capacity exceeded (32)" | Add, Enqueue, Push | Count == 32 |
| `InvalidOperationException` "Stack Empty" / "Queue Empty" | Pop, Dequeue, Peek | Count == 0 |
| `ArgumentOutOfRangeException` | RemoveAt | Invalid index |

**No silent failures**: All error conditions throw exceptions or return false (Try- variants).

## Struct lifetime and async

**Constraint**: Ref structs cannot be used across await boundaries.

```csharp
async Task ProcessAsync() {
    var list = new InlineList32<int>();
    list.Add(1);
    
    await Task.Delay(1);  // ❌ Compiler error: ref struct cannot cross await
}
```

**Workaround**: Convert to array:
```csharp
async Task ProcessAsync() {
    var list = new InlineList32<int>();
    list.Add(1);
    
    int[] array = list.AsSpan().ToArray();  // Copy to managed array
    await Task.Delay(1);  // ✅ OK, array is managed
}
```

## Enumerator modification safety

**Constraint**: Modifying collection during enumeration causes undefined behavior.

```csharp
var list = new InlineList32<int>();
list.Add(1); list.Add(2);

foreach (var item in list) {
    list.Add(3);  // ❌ Modifying during enumeration
}
```

**Mitigation**: Snapshot or use manual loop:
```csharp
var snapshot = list.AsSpan().ToArray();
foreach (var item in snapshot) {
    list.Add(3);  // ✅ Safe
}
```

## Ref return lifetime safety

**Constraint**: Ref returns must not outlive the source collection.

```csharp
ref int GetRef(InlineList32<int>& list) {  // ❌ Compiler prevents this
    return ref list[0];
}
```

The compiler statically prevents this pattern. Ref returns cannot escape stack scope.

## Recommended limits

| Scenario | Recommended size | Rationale |
|----------|------------------|-----------|
| Small collections | ≤ 16 elements | Conservative stack usage |
| Moderate depth recursion | 5-10 collections | ~400-600 bytes/frame |
| Deep recursion | Consider List<T> | Stack pressure |
| Large element types (>16B) | Reconsider approach | Copy cost dominates |

## When NOT to use InlineCollections

1. Collections often exceed 32 elements
2. Managed types (string, class) required
3. Thread-safety essential
4. Dynamic capacity needed
5. API compatibility with List<T> required
6. Async/await usage across collection lifetime
7. Stack depth > 5000 levels
