# Performance

This document explains the performance model, benchmark methodology, and results for InlineCollections.

## Performance goals

1. **Zero allocations** on the hot path for operations within capacity.
2. **Minimal call overhead** via aggressive inlining.
3. **Cache locality** through contiguous inline storage.
4. **Predictable latency** with O(1) or O(n) guarantees.

## Zero-allocation philosophy

InlineCollections stores 32 elements inline, eliminating the need to allocate on the heap. 

| Collection | Heap Allocations |
|:---|:---:|
| **InlineList32<int>** | **0** |
| List<int>(capacity:8) | 1 |
| **InlineStack32<int>** | **0** |
| Stack<int>() | 1 |
| **InlineQueue32<int>** | **0** |
| Queue<int>() | 1 |

## Benchmark methodology

### BenchmarkDotNet configuration

```csharp
[SimpleJob(RuntimeMoniker.Net80, invocationCount: 1000)]
[MemoryDiagnoser]
[MinColumn, MaxColumn, MeanColumn, MedianColumn, StdDevColumn]
public class InlineList32Benchmark { ... }
```

- Runtime: .NET 8.0
- Invocation count: 1000 per benchmark iteration
- Memory diagnoser: tracks allocations and Gen2 collections
- Columns: min, max, mean, median, standard deviation

### Running benchmarks

```bash
cd benchmarks/InlineCollections.Benchmarks
dotnet run -c Release
```

This produces CSV/JSON output in `BenchmarkDotNet.Artifacts/results/`.

## Benchmark results

### InlineList32 vs List<T>

| Operation | InlineList32 | List<T> | Ratio | Allocation |
|:---|:---:|:---:|:---:|:---:|
| **Constructor (Span/Enum)** | 13.94 ns | 102.46 ns | **7.4x ⚡** | 0 vs 1 |
| **AddRange (Bulk Add)** | 21.21 ns | 98.72 ns | **4.7x ⚡** | 0 vs 1 |
| **Add (Single)** | 17.29 ns | 60.50 ns | **3.5x ⚡** | 0 vs 1 |
| **Contains (Search)** | 9.11 ns | 21.69 ns | **2.4x ⚡** | 0 vs 0 |
| **Remove (Middle)** | 83.58 ns | 163.86 ns | **2.0x ⚡** | 0 vs 1 |
| **Insert (Middle)** | 45.33 ns | 84.89 ns | **1.9x ⚡** | 0 vs 1 |
| **Indexer (Access)** | 9.43 ns | 9.97 ns | 1.1x | 0 vs 0 |

**Key Insights**:
- **7.4x Speedup** in construction: Eliminating heap allocation and using direct span copy.
- **Bulk Operations**: AddRange is significantly faster as it avoids internal resizing and allocations.
- **Zero GC Pressure**: All operations maintain zero bytes allocated.

### 2. InlineStack32 vs Stack<T>

| Operation | InlineStack32 | Stack<T> | Ratio | Allocation |
|:---|:---:|:---:|:---:|:---:|
| **Foreach Iteration** | 20.68 ns | 155.01 ns | **7.5x ⚡** | 0 vs 0 |
| **Creation (New)** | 8.36 ns | 37.67 ns | **4.5x ⚡** | 0 vs 1 |
| **Push** | 17.29 ns | 51.03 ns | **3.0x ⚡** | 0 vs 1 |
| **TryPush/Pop Cycle** | 12.14 ns | 15.60 ns | **1.3x ⚡** | 0 vs 0 |
| **Pop/Peek** | 15.59 ns | 19.61 ns | **1.3x ⚡** | 0 vs 0 |

**Key Insights**:
- **Iteration Dominance**: 7.5x faster foreach loops due to the efficient `ref struct` enumerator.
- **Allocation-Free Push**: 3x speedup by keeping the storage on the stack.
- **BCL Overhead**: Even simple Peek/Pop are slower in BCL due to array indirection.

### 3. InlineQueue32 vs Queue<T>

| Operation | InlineQueue32 | Queue<T> | Ratio | Allocation |
|:---|:---:|:---:|:---:|:---:|
| **Foreach Iteration** | 2.22 μs | 11.99 μs | **5.4x ⚡** | 0 vs 1 |
| **Clear** | 415.7 ns | 684.8 ns | **1.6x ⚡** | 0 vs 1 |
| **WrapAround Performance** | 481.0 ns | 594.2 ns | **1.2x ⚡** | 0 vs 1 |
| **Enqueue/Dequeue Cycle** | 6.80 μs | 8.30 μs | **1.2x ⚡** | 0 vs 1 |

**Key Insights**:
- **Massive Iteration Gains**: 5.4x faster iteration over circular buffer.
- **Efficient Wrapping**: Using bitwise masking (`& 31`) provides a measurable edge over standard modulo operations.
- **Predictable Performance**: Zero allocations even during heavy enqueue/dequeue cycles.

## Cache behavior

### Inline storage advantage

InlineList32<int> stores 128 bytes (32 ints) inline:
```
Cache line: 64 bytes (typical)
Fit: ~2 cache lines
Memory access: 1-2 L1 cache misses (worst case), then cache hits
```

List<T> has array on heap:
```
Reference on stack: pointer
Array on heap: separate allocation, different cache line
Memory access: dereference (cache miss to heap), then array access
```

For small iterations, InlineCollections benefit from spatial locality.

### Measurement

```csharp
// Benchmark: iterate 100 times
var list = new InlineList32<int>(data32);  // 32 ints
int sum = 0;
for (int i = 0; i < 100; i++)
    sum += list[i % 32];
```

InlineList32 and List<T> perform nearly identically (both direct memory access), but InlineList32 has advantage if the struct is kept in L1 cache.

## Inlining and JIT

### AggressiveInlining

Hot methods are marked with `[MethodImpl(MethodImplOptions.AggressiveInlining)]`:

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public void Add(T item)
{
    if ((uint)_count >= Capacity) ThrowFull();
    Unsafe.Add(ref _buffer[0], _count++) = item;
}
```

This tells the JIT to inline the method even if the heuristics would normally prevent it.

### JIT-compiled output

For `Add()`, the JIT generates (x64 pseudocode):
```asm
cmp     [rsp+offset], 32    ; if _count < 32
jge     throw_full
mov     [rsp+rax*4], rcx    ; Write element at index
inc     [rsp+offset]        ; _count++
ret
```

Nearly branch-free for the common case (no allocation, no indirection).

## Struct size

### Struct sizes

| Collection | Element | Size |
|-----------|---------|------|
| InlineList32<int> | 4B | 132B |
| InlineList32<long> | 8B | 260B |
| InlineStack32<int> | 4B | 132B |
| InlineQueue32<int> | 4B | 140B |

Memory movement overhead depends on CPU architecture and scales with struct size.

### Recommendation

Avoid passing by value in hot loops. Use `ref` parameter:

```csharp
// ❌ Bad: copies struct
void Process(InlineList32<int> list) { ... }

// ✅ Good: no copy
void Process(ref InlineList32<int> list) { ... }
```

## Allocation-free guarantee

The collections guarantee zero allocations for operations within capacity:

```csharp
GC.TotalMemory(false, out long before);
var list = new InlineList32<int>();
for (int i = 0; i < 32; i++)
    list.Add(i);
GC.TotalMemory(false, out long after);

Debug.Assert(before == after);  // No allocations
```

## When benchmarks mislead

Microbenchmarks measure isolated operations. In real workloads:
- CPU caches may be cold
- Branch prediction history differs
- Interleaving with other operations changes access patterns
- Real collections may have better lock-free properties

**Guidance**: Use benchmarks to validate assumptions, but always profile real workloads.

## Comparison with standard collections

| Aspect | InlineCollections | BCL (List<T>, Stack<T>, Queue<T>) |
|--------|------------------|-----------------------------------|
| Allocation | 0 | 1 per instance |
| Capacity | Fixed 32 | Dynamic |
| Indexer overhead | None | Bounds check |
| ref return | Yes | No (List only) |
| Thread-safe | No | No |
| Suitable for | Hot paths, small sets | General purpose |

## Performance summary

**Best for InlineCollections**:
- Add/Enqueue when creating new collections
- Tight loops with repeated small collections
- Stack frames with limited depth

**Best for BCL collections**:
- Large or unbounded working sets
- API compatibility required
- Thread-safety needed
- Reference type semantics required
