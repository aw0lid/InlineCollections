[![NuGet Version](https://img.shields.io/nuget/v/InlineCollections.svg?style=flat-square)](https://www.nuget.org/packages/InlineCollections/)
![.NET 8.0+](https://img.shields.io/badge/.NET-8.0%2B-blue)
![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)
![Allocations: Zero](https://img.shields.io/badge/Allocations-Zero-green)
![Performance: Ultra--Low--Latency](https://img.shields.io/badge/Performance-Ultra--Low--Latency-orange)


# ⚡ InlineCollections

InlineCollections provides high-performance, zero-allocation collection primitives for .NET with a fixed capacity of 32 elements. The collections are implemented as `ref struct` types optimized for ultra-low latency scenarios where heap allocations must be eliminated.

## 🚀 Overview

`InlineList32<T>`, `InlineStack32<T>`, and `InlineQueue32<T>` provide stack-allocated storage via the `InlineArray` language feature (C# 12+), enabling:

- ✨ Zero heap allocations for the fast path
- 🏎️ Minimal GC pressure
- 🎯 Predictable memory layout for cache optimization
- 🛡️ High-throughput, low-latency execution

---

> [!IMPORTANT]
> **Positioning Statement**: This library is **not a general-purpose replacement** for the standard BCL collections types. Standard collections are designed for flexibility and large datasets. `InlineCollections` are "surgical tools" designed for **High-Performance hot-paths** where the developer has a guaranteed bound on the number of elements (≤ 32) and must eliminate heap allocations to reduce GC pressure and latency.

---

## 🛠️ Getting Started

### 📦 Installation

Add the package to your project via .NET CLI:

```bash
dotnet add package InlineCollections --version 0.1.0
```

## 🚀 Quick Start & Usage

`InlineCollections` are `ref struct` types designed for stack allocation. They ensure **Zero Heap Allocation** for up to 32 elements.

```csharp
using InlineCollections;

// Initialize on stack (Zero Allocation)
var list = new InlineList32<int>();

list.Add(10);
list.Add(20);

// High-performance iteration (Modify in-place via Span)
foreach (ref int item in list.AsSpan())
{
    item += 1; 
}
```

---

## 💡 Why this library exists

Standard .NET collections (`List<T>`, `Stack<T>`, `Queue<T>`) allocate on the heap, requiring GC overhead and cache misses for small working sets. In high-performance scenarios (real-time systems, game engines, network processors, serialization hotspots), this overhead is unacceptable. InlineCollections eliminates allocations by storing 32 elements inline within the struct itself.

> **Performance highlights**
>
>- ⚡ **Zero Allocations** — for up to 32 elements these collections avoid heap allocations.
>- 🗑️ **Reduced GC Pressure** — fewer short-lived allocations means fewer GC cycles and pauses.
>- ⚖️ **Predictable Latency** — bounded-capacity operations reduce variance in execution time.

---

## ✅ When to use

- ⚡ Hot-path code that creates many short-lived small collections
- ⏱️ Real-time systems requiring predictable latency
- 🎮 Game engine frame-local processing (per-frame temporary buffers)
- ⚡ Network packet processing and protocol parsing
- 🔁 Serialization/deserialization buffers where allocations matter
- 🧠 Stack-like or frame-local data with bounded depth


## When NOT to use

- Collections that routinely exceed 32 elements
- Scenarios requiring thread-safety or concurrent access
- Reference-type or nullable element types
- When API compatibility with `List<T>` is required
- Managed heap scenarios where GC pressure is not a primary concern

---

## How it works

### Memory model

Each collection type uses the `InlineArray32<T>` struct, which leverages the `[InlineArray(32)]` attribute to embed 32 elements directly inside the struct. This is a value-type collection stored on the stack (when not captured in a reference type).

- Inline storage: 32 elements stored as struct fields
- No heap allocation
- `ref struct` semantics (no boxing, no reference storage)

### Constraints

- Unmanaged element types only (constraint: `T : unmanaged, IEquatable<T>`)
- Fixed capacity of exactly 32 elements
- Throws `InvalidOperationException` when capacity is exceeded
- Value-type semantics: copies on assignment/parameter passing

## Collections provided

### InlineList32<T>

A list with a maximum capacity of 32 unmanaged elements.

**Key methods:**
- `Add(T item)` — add to end; throws if full
- `TryAdd(T item)` — add to end; returns false if full
- `AddRange(ReadOnlySpan<T> items)` — bulk add
- `Insert(int index, T item)` — insert at index
- `Remove(T item)` — remove first occurrence
- `RemoveAt(int index)` — remove by index
- `T this[int index]` — indexer with ref return for in-place modification
- `Span<T> AsSpan()` — get current elements as a span
- `Contains(T item)` — linear search
- `Clear()` — empty the list

**Example:**
```csharp
var list = new InlineList32<int>();
list.Add(1);
list.Add(2);
list.Add(3);
int value = list[0];  // 1

var span = list.AsSpan();
foreach (var item in span) {
    Console.WriteLine(item);
}

foreach (var item in list) {
    Console.WriteLine(item);
}
```

### InlineStack32<T>

LIFO (Last-In-First-Out) collection with maximum capacity of 32 elements.

**Key methods:**
- `Push(T item)` — push to stack; throws if full
- `TryPush(T item)` — push; returns false if full
- `T Pop()` — pop and return; throws if empty
- `bool TryPop(out T result)` — pop safely
- `ref T Peek()` — return ref to top without removing; throws if empty
- `Span<T> AsSpan()` — get all elements (in insertion order)
- `Clear()` — empty the stack

**Example:**
```csharp
var stack = new InlineStack32<int>();
stack.Push(10);
stack.Push(20);
stack.Push(30);

int top = stack.Pop();  // 30
int next = stack.Pop(); // 20

ref int peek = ref stack.Peek();
peek = 100;  // modify in-place

foreach (var item in stack) {
    Console.WriteLine(item);  // Iterates in reverse order (LIFO)
}
```

### InlineQueue32<T>

FIFO (First-In-First-Out) collection with maximum capacity of 32 elements. Internally uses a circular buffer for O(1) enqueue/dequeue.

**Key methods:**
- `Enqueue(T item)` — add to back; throws if full
- `TryEnqueue(T item)` — add safely; returns false if full
- `T Dequeue()` — remove and return from front; throws if empty
- `bool TryDequeue(out T result)` — dequeue safely
- `ref T Peek()` — return ref to front without removing; throws if empty
- `Clear()` — empty the queue

**Example:**
```csharp
var queue = new InlineQueue32<int>();
queue.Enqueue(1);
queue.Enqueue(2);
queue.Enqueue(3);

int first = queue.Dequeue();  // 1
int second = queue.Dequeue(); // 2

ref int front = ref queue.Peek();
front = 999;  // modify in-place

foreach (var item in queue) {
    Console.WriteLine(item);  // Iterates in FIFO order
}
```

## Limitations and exceptions

- **Fixed capacity**: Exactly 32 elements; exceeding capacity throws `InvalidOperationException`.
- **Unmanaged types only**: `T` must satisfy `T : unmanaged, IEquatable<T>`.
- **Value semantics**: Assignment and parameter passing copy the entire struct.
- **Struct size**: Each collection is 32 * sizeof(T) bytes plus overhead. Large `T` types increase stack usage.
- **ref struct**: Cannot be stored in fields of reference types or classes; cannot be boxed.
- **No null elements**: Elements must be valid unmanaged values.
- **Exceptions**:
  - `InvalidOperationException` — capacity exceeded or collection is empty (on Pop/Peek/Dequeue without Try- variant)
  - `ArgumentOutOfRangeException` — invalid index (RemoveAt)

## Performance characteristics

All operations are O(1) constant time except:
- `Remove(T item)` — O(n) linear search and shift
- `RemoveAt(int index)` — O(n) shifts remaining elements
- `Insert(int index, T item)` — O(n) shifts elements to the right

Indexer access (`this[int index]`) and Peek/Pop operations have zero bounds checking and are aggressively inlined.

For detailed benchmarks and comparisons with `List<T>`, `Stack<T>`, and `Queue<T>`, see [docs/benchmarks.md](docs/benchmarks.md).



## Documentation

- [Architecture](docs/architecture.md) — internal design and memory layout
- [Design Philosophy](docs/design-philosophy.md) — principles and goals
- [Memory Model](docs/memory-model.md) — stack vs heap, value semantics, ref safety
- [Collections](docs/collections/) — per-collection API reference and examples
  - [InlineList32](docs/collections/inline-list.md)
  - [InlineStack32](docs/collections/inline-stack.md)
  - [InlineQueue32](docs/collections/inline-queue.md)
- [Performance](docs/performance.md) — benchmark methodology and results
- [Limitations](docs/limitations.md) — hard constraints and exceptions
- [When to Use](docs/when-to-use.md) — recommended scenarios
- [When Not to Use](docs/when-not-to-use.md) — scenarios to avoid
- [FAQ](docs/faq.md) — common questions


## Benchmarks

BenchmarkDotNet results comparing InlineCollections with standard BCL collections are available in the `benchmarks/` directory. Run:

```bash
dotnet run --project benchmarks/InlineCollections.Benchmarks -c Release
```

Key findings:
- **Add operations**: InlineList32 is 3-5x faster than List<T> for small collections (zero allocations)
- **Indexer access**: Near-identical performance to List<T> (both use direct memory access)
- **Memory**: Zero heap allocations vs one allocation for List<T>

See [docs/benchmarks.md](docs/benchmarks.md) for full results.

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


