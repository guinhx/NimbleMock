# Getting Started with NimbleMock

NimbleMock is a zero-allocation, source-generated C# mocking library designed for exceptional performance and developer experience.

## Installation

```bash
dotnet add package NimbleMock
```

## Quick Start

```csharp
using NimbleMock;

public interface IUserRepository
{
    User GetById(int id);
    Task<bool> SaveAsync(User user);
}

// Create a mock
var mock = Mock.Of<IUserRepository>()
    .Setup(x => x.GetById(1), new User { Id = 1, Name = "Alice" })
    .SetupAsync(x => x.SaveAsync(default!), true)
    .Build();

// Use the mock
var user = mock.Object.GetById(1);

// Verify calls
mock.Verify(x => x.GetById(1)).Once();
```

## Key Features

- **Zero-allocation** for typical scenarios
- **34x faster** than Moq in mock creation, **3x faster** in verification
- **Fluent API** with intelligent type inference
- **First-class async** support for `Task<T>` and `ValueTask<T>`
- **Partial mocks** for large interfaces
- **Static dependencies** via `Shim.For<T>()` wrapper pattern
- **Generics & nested hierarchies** with full type inference
- **Lie-proofing** validation against real API endpoints
- **Compile-time** analyzers

## Next Steps

- See [API Reference](api-reference.md) for detailed API documentation
- See [Partial Mock Examples](partial-mock-examples.md) for real-world patterns
- Check [Performance](performance.md) for benchmark results
- Read [Migration Guide](migration-guide.md) to migrate from Moq

