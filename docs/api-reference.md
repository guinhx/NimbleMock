# API Reference

## Mock.Of<T>()

Creates a new mock builder for the specified interface.

```csharp
var mock = Mock.Of<IUserRepository>()
    .Setup(x => x.GetById(1), user)
    .Build();
```

## Mock.Partial<T>()

Creates a partial mock where only specified methods are mocked. Unmocked methods will throw `NotImplementedException`.

```csharp
var mock = Mock.Partial<ILargeService>()
    .Only(x => x.GetData(1), data)
    .Build();
```

## MockBuilder<T>

### Setup<TResult>(Expression, TResult)

Configures a method to return a value.

```csharp
.Setup(x => x.GetById(1), user)
```

### SetupAsync<TResult>(Expression, TResult)

Configures an async method returning `Task<TResult>` or `ValueTask<TResult>` to return a value.

```csharp
.SetupAsync(x => x.SaveAsync(default!), true)
```

### Throws<TException>(Expression, Exception)

Configures a method to throw an exception.

```csharp
.Throws(x => x.HealthCheck(), new TimeoutException())
```

## PartialMock<T>

A builder for configuring partial mock method setups. Only specified methods will be mocked; others will throw `NotImplementedException`.

### Only<TResult>(Expression, TResult)

Configures a method to be mocked, returning the specified value.

```csharp
var mock = Mock.Partial<ILargeService>()
    .Only(x => x.GetData(1), data)
    .Build();
```

## Nested Property Mocking

For mocking deep property chains like `IOptions<T>.Value.Database.ConnectionString`, use the composition pattern:

### Compose Nested Interface Mocks

```csharp
// 1. Mock the innermost interface
var dbMock = Mock.Of<IDatabaseConfig>()
    .Setup(x => x.ConnectionString, "Server=localhost")
    .Setup(x => x.MaxConnections, 100)
    .Build();

// 2. Mock the outer interface, passing the inner mock
var configMock = Mock.Of<IAppConfig>()
    .Setup(x => x.Database, dbMock.Object)
    .Build();

// 3. Access the nested property
string conn = configMock.Object.Database.ConnectionString; // "Server=localhost"
```

### SetupNested (Single-level Shortcut)

For single-level property access, use `SetupNested`:

```csharp
var mock = Mock.Of<IDatabaseConfig>()
    .SetupNested(x => x.ConnectionString, "Server=localhost")
    .Build();
```

### POCO Properties (Non-Interface)

For concrete classes, set up the value directly:

```csharp
var settings = new AppSettings 
{ 
    Database = new DatabaseSettings { Server = "localhost" }
};

var mock = Mock.Of<IAppSettingsOptions>()
    .Setup(x => x.Value, settings)
    .Build();

// Access nested POCO properties
string server = mock.Object.Value.Database.Server; // "localhost"
```

## VerifiableMock<T>


### Object

Gets the mocked object instance.

```csharp
var repo = mock.Object;
```

### Verify<TResult>(Expression)

Starts verification for a method call.

```csharp
mock.Verify(x => x.GetById(1)).Once();
```

### VerifyNoOtherCalls()

Verifies that no other methods were called.

```csharp
mock.VerifyNoOtherCalls();
```

## VerifyBuilder<T>

### Once()

Verifies the method was called exactly once.

### Times(int)

Verifies the method was called a specific number of times.

```csharp
mock.Verify(x => x.GetById(1)).Times(3);
```

### Never()

Verifies the method was never called.

```csharp
mock.Verify(x => x.Delete(2)).Never();
```

### AtLeast(int)

Verifies the method was called at least the specified number of times.

```csharp
mock.Verify(x => x.SendEmail(default!)).AtLeast(1);
```

### WithArg<TArg>(int)

Verifies method arguments.

```csharp
mock.Verify(x => x.SendAsync(default!, default!))
    .WithArg<string>(0)
    .Matching(email => email.Contains("@"));
```

## Shim.For<T>() - Static Dependencies

For mocking static dependencies like `DateTime`, use the **wrapper pattern**. Create an interface for the static dependency, then mock the interface.

```csharp
// 1. Define a shim interface
public interface IDateTimeProvider
{
    DateTime Now { get; }
    DateTime UtcNow { get; }
}

// 2. Create a default implementation for production
public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime Now => DateTime.Now;
    public DateTime UtcNow => DateTime.UtcNow;
}

// 3. In tests, mock the interface
var fixedDate = new DateTime(2024, 1, 1);
var mock = Shim.For<IDateTimeProvider>()
    .Replace(d => d.Now, fixedDate)
    .Replace(d => d.UtcNow, fixedDate)
    .Build();

// Use mock.Object in your code under test
var service = new MyService(mock.Object);

// Verify calls
mock.Verify(d => d.Now).Once();
```

> **Note**: `Shim.For<T>()` is syntax sugar for `Mock.Of<T>()` - both work identically.


## Advanced Examples

### Generics with Type Inference

```csharp
public interface IRepository
{
    IQueryable<T> Query<T>();
}

var mock = Mock.Of<IRepository>()
    .Setup(x => x.Query<User>(), users.AsQueryable())
    .Setup(x => x.Query<Order>(), orders.AsQueryable())
    .Build();

var userQuery = mock.Object.Query<User>();
var filtered = userQuery.Where(u => u.IsActive).ToList();
```

### Nested Mock Hierarchies

```csharp
public interface IRepository
{
    IQueryable<T> Query<T>();
    Task<T> GetByIdAsync<T>(int id);
}

var mock = Mock.Of<IRepository>()
    .Setup(x => x.Query<User>(), users.AsQueryable())
    .SetupAsync(x => x.GetByIdAsync<User>(1), user)
    .Chain<IRepository>(x => x.Query<User>())
    .Build();
```

## Lie-Proofing

Validate that your mocks match real API shapes using `LieProofing.AssertMatchesReal<T>`:

```csharp
var result = await LieProofing.AssertMatchesReal<IApiService>(
    "https://api.staging.example.com/v1");
    
if (!result.IsValid)
{
    foreach (var mismatch in result.Mismatches)
    {
        Console.WriteLine($"Mismatch: {mismatch}");
    }
}
```

