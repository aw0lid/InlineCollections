# FAQ

## General questions

### Q: Why are these collections value types (ref structs)?

**A**: Value types enable stack allocation and inline storage. Stack allocation avoids heap allocation and GC overhead. Ref structs prevent the stack-allocated memory from being accidentally captured in reference types or async contexts, which would cause use-after-free bugs.

### Q: Will InlineCollections replace `List<T>` in my code?

**A**: No. InlineCollections are specialized for hot-path scenarios with small, bounded collections (≤ 32 elements). They are **not** drop-in replacements. Use `List<T>` by default; only adopt InlineCollections after profiling shows allocation is a bottleneck.

### Q: Why exactly 32 elements?

**A**: Several reasons:
1. **Cache efficiency**: 32 int64s = 256 bytes ≈ 4 cache lines (typical prefetch range)
2. **Stack reasonable**: ~130 bytes per collection (int-sized)
3. **Power of 2**: Enables fast circular buffer wrap-around in `InlineQueue32` (bitwise AND vs modulo)
4. **Practical**: Fits most bounded scenarios (message headers, frame-local state, etc.)

### Q: What happens when I exceed capacity?

**A**: `InvalidOperationException` is thrown immediately. No silent failure, no degradation to heap-backed storage. This is intentional: if you're exceeding 32 elements, you should use `List<T>` instead.

## Type constraints

### Q: Why `T : unmanaged, IEquatable<T>`?

**A**: 
- **`unmanaged`**: Elements cannot contain managed references (string, class). This ensures stack allocation is safe; no GC tracing needed.
- **`IEquatable<T>`**: Required for comparison-based operations (Remove, Contains). Using IEquatable avoids boxing.

### Q: Can I use `string`, classes, or interfaces?

**A**: No. `string` is a managed type. Classes and interfaces are reference types. InlineCollections cannot store managed references because the collection is stack-allocated and not visible to the GC.

**Workaround**: Use `List<T>` or store indices into a separate managed array:

```csharp
string[] strings = new[] { "hello", "world" };
var indices = new InlineList32<int>();
indices.Add(0);  // Reference strings[0]
indices.Add(1);  // Reference strings[1]

foreach (int idx in indices.AsSpan()) {
    Console.WriteLine(strings[idx]);
}
```

### Q: Can I use nullable types like `int?`?

**A**: No. `Nullable<T>` is a managed wrapper. Use a separate flag or use `List<T>`.

```csharp
// ❌ Won't compile
var list = new InlineList32<int?>();

// ✅ Alternative: sentinel value
var list = new InlineList32<int>();
list.Add(0);  // Use 0 to mean "null"

// ✅ Better alternative
var list = new List<int?>();
```

## Performance questions

### Q: How much faster is InlineCollections than `List<T>`?

**A**: Depends on operation:
- **Construction**: 28x faster (eliminates allocation)
- **Add operations**: 4-11x faster (eliminates allocation)
- **Indexer access**: 1.0x (identical; both use direct memory)
- **Push/Pop (stack)**: 25-60x faster (eliminates allocation)
- **Enqueue/Dequeue (queue)**: 17-40x faster (eliminates allocation + indirect access)

**Key**: Advantage is primarily from eliminating allocations. If your workload is memory-limited and allocation is the bottleneck, gains are substantial.

### Q: What's the memory copy cost?

**A**: Depends on element type:

| Type | Struct size | Copy time |
|------|------------|-----------|
| InlineList32<int> | 132B | ~3ns |
| InlineList32<long> | 260B | ~6ns |
| InlineStack32<Guid> | 516B | ~12ns |

Copy happens on assignment and parameter passing. Use `ref` parameters in hot loops to avoid copying.

### Q: Is InlineCollections faster than arrays?

**A**: No. Arrays are faster. But arrays lack collection semantics (Count, Add, Remove, etc.). InlineCollections trade small performance overhead for convenience.

```csharp
// Fastest (if you can use raw arrays)
int[] arr = new int[32];

// Convenient (InlineList32)
var list = new InlineList32<int>();

// Slower (dynamic growth)
var list = new List<int>();
```

### Q: Is GC pausing an issue?

**A**: Not with InlineCollections. Stack allocation avoids GC entirely. No allocations = no GC pressure = no GC pauses from these collections.

## Concurrency

### Q: Are InlineCollections thread-safe?

**A**: No. They are not synchronized and not atomic.

```csharp
// ❌ Race condition
var list = new InlineList32<int>();
Task.Run(() => list.Add(1));
Task.Run(() => list.Add(2));  // Undefined behavior
```

**Workaround**: Use locks or concurrent collections:

```csharp
// ✅ Thread-safe
var queue = new ConcurrentQueue<int>();
Task.Run(() => queue.Enqueue(1));
Task.Run(() => queue.Enqueue(2));
```

### Q: Can I use these in async/await?

**A**: No. Ref structs cannot cross `await` boundaries.

```csharp
// ❌ Compiler error
async Task MyAsync() {
    var list = new InlineList32<int>();
    await Task.Delay(1);  // Error
}

// ✅ Workaround: convert to array
async Task MyAsync() {
    var array = new[] { 1, 2, 3 };
    await Task.Delay(1);
    Process(array);
}
```

## Usage patterns

### Q: Should I pass these collections by value or by ref?

**A**: **By ref** in hot paths to avoid copying:

```csharp
// ❌ Copies 132 bytes
void Process(InlineList32<int> list) { ... }

// ✅ No copy
void Process(ref InlineList32<int> list) { ... }
```

In non-critical paths, by-value is OK.

### Q: How do I handle exceeding capacity?

**A**: Design your code to never exceed 32. If that's impossible, use `List<T>`.

**Pattern**:
```csharp
var list = new InlineList32<int>();
if (!list.TryAdd(value)) {
    // Handle: flush, grow, or reject
    FlushAndProcess(ref list);
    list.Clear();
    list.Add(value);
}
```

### Q: How do I iterate safely?

**A**: Use `AsSpan()` for safety:

```csharp
var list = new InlineList32<int>();
list.Add(1);
list.Add(2);

// Safe span iteration
var span = list.AsSpan();
foreach (var item in span) {
    // ...
}

// Also safe: GetEnumerator()
foreach (var item in list) {
    // ...
}

// Unsafe: modifying during iteration
foreach (var item in list) {
    list.Add(3);  // ❌ Undefined behavior
}
```

### Q: Can I use LINQ?

**A**: Partially. You can convert to span or array:

```csharp
var list = new InlineList32<int>();
list.Add(1);
list.Add(2);
list.Add(3);

// ✅ Works
var sum = list.AsSpan().Sum();
var doubled = list.AsSpan().Select(x => x * 2).ToList();

// ❌ Won't work (ref struct doesn't implement IEnumerable<>)
var result = list.Select(x => x * 2);  // Compiler error
```

## Troubleshooting

### Q: "Cannot use ref struct here" compiler error?

**A**: You're trying to use a ref struct where a reference type is required. Ref structs cannot be:
- Stored in class fields
- Boxed
- Used in interfaces
- Passed to methods expecting `IEnumerable<T>`

**Solution**: Use `List<T>` or convert to array/span.

### Q: "The ref struct can only be used inside a method"?

**A**: You declared a ref struct at class scope or tried to return it from a method. Ref structs are stack-only.

**Solution**:
```csharp
// ❌ Wrong
ref struct MyCollection { }
class Wrapper {
    MyCollection field;  // ❌ Compiler error
}

// ✅ Correct (local scope)
class Processor {
    void Method() {
        var col = new InlineList32<int>();  // OK
    }
}
```

### Q: Performance isn't improving?

**A**: 
1. **Check allocation**: Use profiler to verify allocation is the bottleneck
2. **Check element size**: Large `T` might negate gains
3. **Check copy overhead**: If passing by value in hot loop, use `ref`
4. **Check bounds**: Are you hitting 32-element limit?

### Q: Getting `InvalidOperationException` when full?

**A**: Capacity is 32. Either:
1. Design to stay under 32 (intended use)
2. Use `TryAdd()` and handle failure
3. Switch to `List<T>` for unbounded growth

```csharp
if (!list.TryAdd(value)) {
    // Use List<T> instead
    var fallback = new List<int>(list.AsSpan());
    fallback.Add(value);
}
```

## Documentation and support

### Q: Where's more documentation?

**A**: See the `/docs` folder:
- [Architecture](architecture.md) — internal design
- [Design Philosophy](design-philosophy.md) — why these choices
- [Memory Model](memory-model.md) — stack/heap, ref semantics
- [Collections](collections/) — per-collection API reference
- [Performance](performance.md) — benchmarks and methodology
- [Limitations](limitations.md) — hard constraints
- [When to Use](when-to-use.md) — recommended scenarios
- [When Not to Use](when-not-to-use.md) — anti-patterns

### Q: How do I report bugs?

**A**: Create an issue on GitHub with:
1. Minimal reproducible example
2. Expected vs actual behavior
3. Environment (.NET version, OS)
4. Stack trace (if exception)
