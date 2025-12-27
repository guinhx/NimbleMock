# NimbleMock

**Zero-allocation, source-generated C# mocking library for exceptional developer experience.**

[![NuGet](https://img.shields.io/nuget/v/NimbleMock.svg)](https://www.nuget.org/packages/NimbleMock/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![GitHub](https://img.shields.io/github/stars/guinhx/NimbleMock?style=social)](https://github.com/guinhx/NimbleMock)

## Why NimbleMock?

**34× as fast** as Moq, **7× as fast** as NSubstitute in mock creation. **67% less memory** allocation.

```csharp
// Before (Moq) - 48,812ns, 10.37 KB
var mock = new Mock<IUserRepository>();
mock.Setup(x => x.GetById(1)).Returns(user);
mock.Setup(x => x.SaveAsync(It.IsAny<User>())).ReturnsAsync(true);
var repo = mock.Object;

// After (NimbleMock) - 1,415ns, 3.45 KB
var mock = Mock.Of<IUserRepository>()
    .Setup(x => x.GetById(1), user)
    .SetupAsync(x => x.SaveAsync(default!), true)
    .Build();
```

### Key Features

- ✅ **Stack-allocated** builders with arrays - zero GC pressure
- ✅ **Source generators** create compile-time proxies (no Castle.DynamicProxy)
- ✅ **Fluent API** with intelligent type inference
- ✅ **First-class async** support (`Task<T>`, `ValueTask<T>`)
- ✅ **Partial mocks** for large interfaces
- ✅ **Roslyn analyzers** detect unverified calls at compile-time
- ✅ **No telemetry** (looking at you, Moq)

## Installation

```bash
dotnet add package NimbleMock
```

## Quick Start

```csharp
using NimbleMock;
using Xunit;

public interface IUserRepository
{
    User GetById(int id);
    Task<bool> SaveAsync(User user);
}

public class UserServiceTests
{
    [Fact]
    public void GetUser_ReturnsFromRepository()
    {
        // Arrange
        var expectedUser = new User { Id = 1, Name = "Alice" };
        var mock = Mock.Of<IUserRepository>()
            .Setup(x => x.GetById(1), expectedUser)
            .Build();

        var service = new UserService(mock.Object);

        // Act
        var result = service.GetUser(1);

        // Assert
        Assert.Equal(expectedUser, result);
        mock.Verify(x => x.GetById(1)).Once();
    }
}
```

## Performance Comparison

| Operation | Moq | NSubstitute | NimbleMock |
|-----------|-----|-------------|------------|
| **Setup** | 48,812 ns | 9,937 ns | **1,415 ns** |
| **Verification** | 1,795 ns | 2,163 ns | **585 ns** |
| **Memory (Setup)** | 10.37 KB | 12.36 KB | **3.45 KB** |
| **Memory (Verify)** | 2.12 KB | 2.82 KB | **0.53 KB** |

*Benchmarks: .NET 8.0.22, x64, RyuJIT AVX2, Windows 11*

## Documentation

- [Getting Started](docs/getting-started.md)
- [API Reference](docs/api-reference.md)
- [Migration Guide](docs/migration-guide.md)
- [Performance](docs/performance.md)

## Contributing

Contributions are welcome! Please read our contributing guidelines and submit pull requests.

## License

MIT © 2025

