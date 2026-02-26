# When to Use InlineCollections

Use InlineCollections when you have verified allocation or latency constraints in hot-path code and need collections with fixed, bounded capacity up to 32 elements.

## Ideal scenarios

### 1. High-frequency message processing

Networks and messaging systems create many short-lived collections. Example: parsing network packets.

```csharp
// Hot-path packet header parsing
var header = new InlineList32<byte>();
while (stream.TryRead(buffer, out _) && header.Count < 32) {
    header.Add(buffer[0]);
}

ref byte magic = ref header[0];
if (magic != ExpectedMagic) throw new InvalidOperationException();
```

**Why**: Each packet creates a collection; eliminating allocation saves 10-20% overhead.

### 2. Game engine per-frame processing

Collect entities, particles, or effects within frame boundaries.

```csharp
// Frame-local entity list
var activeEntities = new InlineList32<Entity>();
foreach (var entity in AllEntities) {
    if (entity.IsActive && entity.IsVisible) {
        activeEntities.Add(entity);
        if (activeEntities.Count >= 32) break;  // Cap at 32
    }
}

// Render all
var span = activeEntities.AsSpan();
for (int i = 0; i < span.Length; i++) {
    renderer.Draw(ref span[i]);
}

activeEntities.Clear();  // Ready for next frame
```

**Why**: Per-frame collections are short-lived. Zero allocation improves frame time consistency.

### 3. Serialization/deserialization buffers

Convert between formats with bounded intermediate storage.

```csharp
// Parse compact message format
var fields = new InlineList32<Field>();
byte* p = buffer;
while (*p != EndMarker && fields.Count < 32) {
    fields.Add(ParseField(ref p));
}

// Validate and process
foreach (ref Field field in fields.AsSpan()) {
    ValidateField(ref field);
}
```

**Why**: Serialization hotspots create many temporary collections. Stack allocation reduces GC pressure.

### 4. Real-time systems (low-latency requirements)

Audio processing, robotics, financial trading: predictable latency required.

```csharp
// Audio sample collection in fixed-size chunks
var chunk = new InlineStack32<float>();
while (chunk.Count < 32 && inputStream.TryRead(out float sample)) {
    chunk.Push(sample);
}

// Process chunk with no latency spikes from allocation
ProcessAudioChunk(ref chunk);
chunk.Clear();
```

**Why**: GC pauses are unacceptable. Zero allocation eliminates GC pauses.

### 5. Protocol parsing with depth limits

Communication protocols often have bounded recursion/nesting.

```csharp
// Parse nested message with max depth 32
var stack = new InlineStack32<Message>();
stack.Push(root);

while (stack.Count > 0) {
    Message msg = stack.Pop();
    ProcessMessage(msg);
    
    if (msg.HasChildren) {
        foreach (var child in msg.Children) {
            if (stack.TryPush(child)) {
                // OK
            } else {
                // Max depth exceeded
                throw new ProtocolViolationException();
            }
        }
    }
}
```

**Why**: Depth-first traversal with fixed bounds. InlineStack32 is perfect.

### 6. Breadth-first search with bounded frontier

Algorithms with bounded working set.

```csharp
// BFS with max frontier of 32 nodes
var queue = new InlineQueue32<Node>();
queue.Enqueue(root);

while (queue.Count > 0) {
    Node node = queue.Dequeue();
    
    if (node.IsGoal) return node;
    
    foreach (var child in node.Children) {
        if (!queue.TryEnqueue(child)) {
            // Frontier full; skip this branch
            Debug.WriteLine("Frontier full");
        }
    }
}
```

**Why**: FIFO processing with predictable memory. No allocation.

### 7. Stack frames with local working sets

Methods that create temporary collections on the stack.

```csharp
void SortLocalData(ReadOnlySpan<int> input) {
    var indices = new InlineList32<int>();
    for (int i = 0; i < input.Length && indices.Count < 32; i++) {
        indices.Add(i);
    }

    // Sort indices by input values
    var span = indices.AsSpan();
    span.Sort((a, b) => input[a].CompareTo(input[b]));

    // Output sorted values
    foreach (int idx in span) {
        Console.WriteLine(input[idx]);
    }
}
```

**Why**: Local collections; short lifetime; zero allocation.

## Performance-sensitive codepaths

Use InlineCollections when:

- **Profiling shows** allocation is a bottleneck (>5% CPU time)
- **GC pauses are measured** at > 1ms in real workloads
- **Working set is naturally small** (typically ≤ 16 elements, max 32)
- **No unbounded growth** expected

## Recommended patterns

### Pattern 1: Frame-local state

```csharp
class Processor {
    void Process(Frame frame) {
        var items = new InlineList32<Item>();
        foreach (var item in frame.Items) {
            if (items.TryAdd(item)) {
                // Added
            } else {
                // Handle cap
                ProcessBatch(ref items);
                items.Clear();
                items.Add(item);
            }
        }
        ProcessBatch(ref items);
    }
}
```

### Pattern 2: Pass by ref to avoid copying

```csharp
void ProcessList(ref InlineList32<int> list) {  // ref parameter
    // No copy of 132 bytes
    foreach (var item in list) {
        Process(item);
    }
}

var myList = new InlineList32<int>();
// ... populate ...
ProcessList(ref myList);  // ✅ No copy
```

### Pattern 3: Use Try- variants for safe bounds

```csharp
var list = new InlineList32<int>();
if (!list.TryAdd(value)) {
    // Handle cap gracefully
    FlushAndClear(ref list);
    list.Add(value);  // Now safe
}
```

### Pattern 4: Snapshot for cross-method calls

```csharp
var list = new InlineList32<int>();
list.Add(1);

// If we need to pass across scope boundaries:
var array = list.AsSpan().ToArray();  // Convert if needed
ProcessAsync(array);  // OK, array is managed
```

## Decision tree

1. Do you need > 32 elements? → Use `List<T>`
2. Is allocation a measured bottleneck? → Yes? Continue
3. Are elements manageable types? → No? Use `List<T>`
4. Is the collection short-lived? → Yes? InlineCollections ✅
5. Are you in hot-path code? → Yes? InlineCollections ✅

## Measurement approach

1. Baseline: measure current performance with `List<T>`
2. Profile: use PerfView or Profiler; identify allocation hot spots
3. Evaluate: estimate improvement with InlineCollections (zero alloc benefit)
4. Benchmark: implement and measure with BenchmarkDotNet
5. Integrate: adopt if measurable improvement (>5% throughput, <5% memory)
