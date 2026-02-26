# Contributing

Thank you for your interest in contributing to InlineCollections! This guide describes how to set up, develop, and submit changes.

## Getting started

### Prerequisites

- .NET 8.0 SDK or later
- Git
- A code editor (Visual Studio, VS Code, JetBrains Rider, etc.)

### Clone and build

```bash
git clone https://github.com/yourusername/InlineCollections.git
cd InlineCollections

dotnet build                              # Build solution
dotnet test                               # Run tests
dotnet run -p benchmarks -c Release       # Run benchmarks
```

## Development workflow

### 1. Create a branch

```bash
git checkout -b feature/your-feature-name
# or
git checkout -b fix/your-bug-fix
```

### 2. Make changes

Edit files in `src/InlineCollections/`:
- `InlineList.cs`
- `InlineStack.cs`
- `InlineQueue.cs`
- `InlineArray.cs` (internal helper)

### 3. Add or update tests

Tests live in `tests/InlineCollections.Tests/`:
- `InlineList.Test.cs`
- `InlineStack.Test.cs`
- `InlineQueue.Test.cs`

Every public method should have test coverage for:
- Normal operation
- Boundary conditions (empty, full)
- Exception conditions
- Edge cases (negative indices, wrap-around, etc.)

**Example test**:
```csharp
[Fact]
public void Add_WithinCapacity_Succeeds()
{
    var list = new InlineList32<int>();
    list.Add(42);
    Assert.Equal(1, list.Count);
    Assert.Equal(42, list[0]);
}

[Fact]
public void Add_ExceedCapacity_Throws()
{
    var list = new InlineList32<int>();
    for (int i = 0; i < 32; i++) list.Add(i);
    
    Assert.Throws<InvalidOperationException>(() => list.Add(999));
}
```

Run tests:
```bash
dotnet test
# or specific test
dotnet test --filter "Add_ExceedCapacity"
```

### 4. Benchmark your changes

If you modify performance-critical code, add or update benchmarks in `benchmarks/InlineCollections.Benchmarks/`:

```csharp
[Benchmark(OperationsPerInvoke = 100)]
public void YourNewOperation()
{
    for (int i = 0; i < 100; i++)
    {
        var list = new InlineList32<int>();
        // Your operation...
    }
}
```

Run benchmarks:
```bash
cd benchmarks/InlineCollections.Benchmarks
dotnet run -c Release
```

Verify:
- Never significant performance regressions
- Allocation count consistent with design
- Comparable to or better than BCL equivalents

### 5. Format and lint

Run `dotnet format`:

```bash
dotnet format
```

This enforces:
- Code style (naming, spacing, indentation)
- Unused imports
- File header comments

CI will fail if formatting doesn't match. Running locally prevents CI delays.

### 6. Commit and push

```bash
git add .
git commit -m "Add support for custom comparers in InlineList32"
git push origin feature/your-feature-name
```

**Commit message guidelines**:
- Start with a verb (Add, Fix, Improve, Refactor)
- Be specific: "Add X" not "Update code"
- Reference issues: "Fixes #123"
- Keep first line < 50 characters

### 7. Create a pull request

On GitHub, open a PR against the `main` branch. Include:

1. **Description**: What does this change do?
2. **Motivation**: Why is it needed?
3. **Testing**: What tests were added/modified?
4. **Performance**: Any benchmark results?
5. **Checklist**: 
   - [ ] Tests added/updated
   - [ ] Benchmarks updated (if perf-critical)
   - [ ] Documentation updated
   - [ ] Code formatted (dotnet format)
   - [ ] No breaking changes (or justified)

## Code style and conventions

### Naming

- `PascalCase` for public types, methods, properties
- `_camelCase` for private fields (with underscore prefix)
- `camelCase` for local variables, parameters

```csharp
public ref struct InlineList32<T> where T : unmanaged, IEquatable<T>
{
    private InlineArray32<T> _buffer;
    private int _count;

    public int Count => _count;

    public void Add(T item)
    {
        int newCount = _count + 1;
        // ...
    }
}
```

### Performance annotations

Mark hot methods with `[MethodImpl(MethodImplOptions.AggressiveInlining)]`:

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public void Add(T item)
{
    if ((uint)_count >= Capacity) ThrowFull();
    Unsafe.Add(ref _buffer[0], _count++) = item;
}
```

Use `[SkipLocalsInit]` on structs to avoid zero-initialization:

```csharp
[SkipLocalsInit]
public ref struct InlineList32<T> where T : unmanaged, IEquatable<T>
{
    // ...
}
```

### Unsafe code

Use `System.Runtime.CompilerServices.Unsafe` only for performance-critical sections:

```csharp
// ✅ Good: no bounds check, high-performance
public ref T this[int index]
{
    get => ref Unsafe.Add(ref Unsafe.AsRef(in _buffer[0]), index);
}

// ❌ Bad: unnecessary unsafe
private string GetName() => unsafe { return "name"; }
```

### Exception handling

Throw clear exceptions with descriptive messages:

```csharp
[DoesNotReturn]
private static void ThrowFull() => 
    throw new InvalidOperationException("InlineList capacity exceeded (32).");

[DoesNotReturn]
private static void ThrowEmpty() => 
    throw new InvalidOperationException("Stack Empty");
```

## Documentation

### XML comments

Add XML documentation to all public members:

```csharp
/// <summary>
/// Adds an object to the end of the <see cref="InlineList32{T}"/>.
/// </summary>
/// <param name="item">The object to add to the list.</param>
/// <exception cref="InvalidOperationException">Thrown when the list is full.</exception>
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public void Add(T item)
{
    if ((uint)_count >= Capacity) ThrowFull();
    Unsafe.Add(ref _buffer[0], _count++) = item;
}
```

### Update docs

If your change affects user-facing behavior, update relevant docs:
- `README.md` — if adding new collection type or major feature
- `docs/collections/` — API documentation
- `docs/performance.md` — if affecting benchmarks
- `docs/limitations.md` — if introducing new constraints

## Testing guidelines

### Correctness tests

Verify API contracts:
```csharp
[Fact]
public void Add_MultipleItems_MaintainsOrder()
{
    var list = new InlineList32<int>();
    list.Add(1);
    list.Add(2);
    list.Add(3);

    Assert.Equal(1, list[0]);
    Assert.Equal(2, list[1]);
    Assert.Equal(3, list[2]);
}
```

### Boundary tests

Test edge cases:
```csharp
[Fact]
public void RemoveAt_FirstItem() { /* ... */ }

[Fact]
public void RemoveAt_LastItem() { /* ... */ }

[Fact]
public void RemoveAt_InvalidIndex_Throws() { /* ... */ }

[Fact]
public void TryAdd_WhenFull_ReturnsFalse() { /* ... */ }
```

### Stress tests

Verify behavior under load:
```csharp
[Fact]
public void PushPopCycle_1000Times_Succeeds()
{
    var stack = new InlineStack32<int>();
    for (int i = 0; i < 1000; i++)
    {
        stack.Push(i);
        Assert.Equal(1, stack.Count);
        int popped = stack.Pop();
        Assert.Equal(i, popped);
        Assert.Equal(0, stack.Count);
    }
}
```

### Memory tests

Verify allocations:
```csharp
[Fact]
public void Add_DoesNotAllocate()
{
    GC.TotalMemory(false, out long before);
    var list = new InlineList32<int>();
    list.Add(1);
    GC.TotalMemory(false, out long after);
    
    Assert.Equal(before, after);  // No allocation
}
```

## CI/CD

### Automated checks

GitHub Actions runs on every push/PR:
1. **Build**: Compile on ubuntu, windows, macos
2. **Tests**: Xunit suite across all platforms
3. **Format**: dotnet format verification
4. **Reproducible build**: Verify deterministic compilation

### Fixing CI failures

**Build failure**: Ensure solution compiles
```bash
dotnet build
```

**Test failure**: Debug locally
```bash
dotnet test --filter "YourTest"
```

**Format failure**: Auto-fix
```bash
dotnet format
```

## Code review process

1. **Automated checks**: CI must pass
2. **Manual review**: Maintainers review code
3. **Feedback**: Address comments or ask questions
4. **Approval**: Maintainers approve changes
5. **Merge**: Contributor or maintainer merges PR

## Release process

1. Update version in `.csproj`
2. Update `CHANGELOG.md` (if it exists)
3. Tag release: `git tag v0.1.0`
4. Push tag: `git push origin v0.1.0`
5. CI publishes to NuGet automatically

## Getting help

- **Questions**: Open a GitHub Discussion
- **Bugs**: Open a GitHub Issue
- **Ideas**: GitHub Discussions or Issues
- **Code review**: Submit a PR

## Code of conduct

- Be respectful and inclusive
- Focus on the code, not the person
- Assume good intent
- Help others learn and grow

## License

By contributing, you agree your code is licensed under the same license as the project (check LICENSE file).

## Additional resources

- [GitHub Flow](https://guides.github.com/introduction/flow/)
- [.NET Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [Microsoft Collection Design Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/collections)
- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)

Thank you for contributing! 🎉
