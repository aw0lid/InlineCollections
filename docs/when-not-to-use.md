# When NOT to Use InlineCollections

InlineCollections are highly specialized. Use standard BCL collections in these scenarios.

## Anti-patterns

### 1. Unbounded or large collections

**Problem**: Fixed capacity of 32 is too limiting.

```csharp
// ❌ Wrong
var ids = new InlineList32<int>();
for (int i = 0; i < 1000; i++) {
    ids.Add(i);  // Throws after 32 elements
}

// ✅ Correct
var ids = new List<int>();
for (int i = 0; i < 1000; i++) {
    ids.Add(i);
}
```

**Recommendation**: Use `List<T>` for unbounded growth.

### 2. Concurrent or thread-safe scenarios

**Problem**: InlineCollections are not thread-safe.

```csharp
// ❌ Wrong
var queue = new InlineQueue32<Message>();
Task.Run(() => queue.Enqueue(msg1));
Task.Run(() => queue.Enqueue(msg2));  // Race condition

// ✅ Correct
var queue = new ConcurrentQueue<Message>();
Task.Run(() => queue.Enqueue(msg1));
Task.Run(() => queue.Enqueue(msg2));  // Thread-safe
```

**Recommendation**: Use `ConcurrentQueue<T>`, `ConcurrentBag<T>`, or manual synchronization.

### 3. API contracts requiring reference types

**Problem**: Ref struct cannot be used in interfaces or reference-type fields.

```csharp
// ❌ Wrong
interface IProcessor {
    void Process(InlineList32<int> items);  // Compiler error
}

class Container {
    public InlineList32<int> Items;  // Compiler error
}

// ✅ Correct
interface IProcessor {
    void Process(List<int> items);
}

class Container {
    public List<int> Items;
}
```

**Recommendation**: Use `List<T>` when API contracts require reference types.

### 4. Async/await crossing collection lifetime

**Problem**: Ref structs cannot cross await boundaries.

```csharp
// ❌ Wrong
async Task ProcessAsync() {
    var list = new InlineList32<int>();
    list.Add(1);
    await SomeAsync();  // ❌ Compiler error
}

// ✅ Correct (if conversion needed)
async Task ProcessAsync() {
    var array = new[] { 1, 2, 3 };
    await SomeAsync();
    ProcessArray(array);
}
```

**Recommendation**: Use `List<T>` or arrays for async scenarios.

### 5. Storing in collections or data structures

**Problem**: Cannot nest ref structs.

```csharp
// ❌ Wrong
var list = new List<InlineList32<int>>();  // Compiler error
var queues = new InlineList32<InlineQueue32<int>>();  // Compiler error

// ✅ Correct
var lists = new List<List<int>>();
```

**Recommendation**: If you need to store collections, use reference types.

### 6. Working with frameworks expecting standard collections

**Problem**: LINQ, serializers, and frameworks assume reference types.

```csharp
// ❌ Wrong (mostly; some methods work)
var list = new InlineList32<int>();
list.Add(1);
list.Add(2);

var json = JsonConvert.SerializeObject(list);  // May not work as expected
var ienumerable = (IEnumerable<int>)list;  // Won't work (ref struct)

// ✅ Correct
var list = new List<int> { 1, 2 };
var json = JsonConvert.SerializeObject(list);  // Works
var ienumerable = (IEnumerable<int>)list;  // Works
```

**Recommendation**: Use `List<T>` for interop with frameworks.

### 7. Managed types (string, classes, interfaces)

**Problem**: Must be unmanaged types.

```csharp
// ❌ Wrong
var names = new InlineList32<string>();  // Won't compile
var objects = new InlineList32<MyClass>();  // Won't compile
var items = new InlineList32<IEnumerable>();  // Won't compile

// ✅ Correct
var names = new List<string>();
var objects = new List<MyClass>();
var items = new List<IEnumerable>();
```

**Recommendation**: Use `List<T>` for managed types.

### 8. Long-lived data structures

**Problem**: Not designed for persistence across method boundaries.

```csharp
// ❌ Anti-pattern
class Repository {
    private InlineList32<Item> items;  // Won't compile anyway

    public void Add(Item item) {
        items.Add(item);  // If it did compile, would be bad
    }
}

// ✅ Correct
class Repository {
    private List<Item> items = new();

    public void Add(Item item) {
        items.Add(item);
    }
}
```

**Recommendation**: Use `List<T>` for persistent collections.

### 9. Deep recursion

**Problem**: Stack pressure with multiple collections per frame.

```csharp
// ❌ Anti-pattern (risky)
void DeepRecursion(int depth) {
    var list1 = new InlineList32<int>();  // 132 bytes
    var list2 = new InlineList32<int>();  // 132 bytes
    var list3 = new InlineList32<int>();  // 132 bytes
    
    if (depth < 5000)
        DeepRecursion(depth + 1);  // Stack overflow risk
}

// ✅ Better
void DeepRecursion(int depth) {
    var list = new List<int>();  // Heap allocated
    
    if (depth < 5000)
        DeepRecursion(depth + 1);
}
```

**Recommendation**: Profile stack usage. Use `List<T>` in deep recursion.

### 10. Dynamic or unpredictable working sets

**Problem**: Cannot know if 32 is enough.

```csharp
// ❌ Wrong (risky)
void ProcessUserInput(string[] commands) {
    var results = new InlineList32<string>();
    foreach (var cmd in commands) {
        results.Add(ExecuteCommand(cmd));  // Might exceed 32
    }
}

// ✅ Correct
void ProcessUserInput(string[] commands) {
    var results = new List<string>();
    foreach (var cmd in commands) {
        results.Add(ExecuteCommand(cmd));  // Unbounded
    }
}
```

**Recommendation**: Use `List<T>` for unpredictable growth.

## Decision matrix

| Requirement | InlineCollections | Standard Collection |
|------------|------------------|-------------------|
| Bounded capacity ≤ 32 | ✅ | ❌ (overkill) |
| Unbounded growth | ❌ | ✅ |
| Zero allocation critical | ✅ | ❌ |
| Thread-safe | ❌ | ✅ (with Concurrent*) |
| Managed types | ❌ | ✅ |
| Framework interop | ❌ | ✅ |
| Hot-path only | ✅ | ❌ (unnecessary) |
| General purpose | ❌ | ✅ |

## Quick checklist

- [ ] Collection size known to be ≤ 32? → Yes? Continue
- [ ] All elements unmanaged types? → Yes? Continue
- [ ] Allocation a measured bottleneck? → Yes? InlineCollections might fit
- [ ] Hot-path code only? → Yes? Consider InlineCollections
- [ ] Any uncertainty? → Use `List<T>` (safer default)

## Default recommendation

**Default to `List<T>`, `Stack<T>`, `Queue<T>`**. Only adopt InlineCollections after:
1. Profiling shows allocation is a bottleneck
2. Working set is naturally bounded ≤ 32
3. Real workload benchmarks show improvement
4. Team understands ref struct constraints
