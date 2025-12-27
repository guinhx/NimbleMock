# Partial Mock Examples

`Mock.Partial<T>()` is perfect for large interfaces where you only want to mock a few methods. Unmocked methods throw `NotImplementedException`.

## When to Use Partial Mocks

- **Large interfaces** with 10+ methods where you only test 2-3
- **SDK clients** (Azure, AWS) with many operations
- **Legacy code** with broad service contracts

---

## Example 1: Large Service Interface

```csharp
// A typical enterprise service with many methods
public interface IOrderService
{
    Order GetById(int id);
    IEnumerable<Order> GetByCustomer(int customerId);
    IEnumerable<Order> GetPending();
    IEnumerable<Order> GetRecent(int days);
    Task<Order> CreateAsync(OrderRequest request);
    Task UpdateAsync(Order order);
    Task CancelAsync(int orderId, string reason);
    Task<decimal> CalculateTotalAsync(int orderId);
    // ... 20 more methods
}

// Test only needs GetById - mock just that one
[Fact]
public void ProcessOrder_UsesOrderFromService()
{
    var testOrder = new Order { Id = 1, Total = 100m };
    
    var mock = Mock.Partial<IOrderService>()
        .Only(x => x.GetById(1), testOrder)
        .Build();

    var processor = new OrderProcessor(mock.Object);
    var result = processor.Process(1);

    Assert.Equal(100m, result.Total);
    mock.Verify(x => x.GetById(1)).Once();
}
```

---

## Example 2: Azure SDK Client Pattern

```csharp
// Azure Blob Storage client interface (abstracted)
public interface IBlobStorageClient
{
    Task<BlobInfo> UploadAsync(string container, string path, Stream content);
    Task<Stream> DownloadAsync(string container, string path);
    Task DeleteAsync(string container, string path);
    Task<bool> ExistsAsync(string container, string path);
    IAsyncEnumerable<BlobItem> ListBlobsAsync(string container);
    Task<BlobProperties> GetPropertiesAsync(string container, string path);
    // ... many more operations
}

[Fact]
public async Task FileProcessor_ChecksExistenceBeforeDownload()
{
    var mock = Mock.Partial<IBlobStorageClient>()
        .Only(x => x.ExistsAsync("docs", "readme.md"), Task.FromResult(true))
        .Only(x => x.DownloadAsync("docs", "readme.md"), Task.FromResult(new MemoryStream() as Stream))
        .Build();

    var processor = new FileProcessor(mock.Object);
    await processor.ProcessFileAsync("docs", "readme.md");

    mock.Verify(x => x.ExistsAsync("docs", "readme.md")).Once();
    mock.Verify(x => x.DownloadAsync("docs", "readme.md")).Once();
}
```

---

## Example 3: Repository with Many Query Methods

```csharp
public interface IUserRepository
{
    User? GetById(int id);
    User? GetByEmail(string email);
    IEnumerable<User> GetByRole(string role);
    IEnumerable<User> GetActive();
    IEnumerable<User> GetInactive();
    IEnumerable<User> Search(string query);
    Task<int> CountAsync();
    Task SaveAsync(User user);
    Task DeleteAsync(int id);
}

[Fact]
public void AuthService_FindsUserByEmail()
{
    var testUser = new User { Id = 1, Email = "test@example.com", IsActive = true };
    
    var mock = Mock.Partial<IUserRepository>()
        .Only(x => x.GetByEmail("test@example.com"), testUser)
        .Build();

    var authService = new AuthService(mock.Object);
    var result = authService.Authenticate("test@example.com", "password");

    Assert.NotNull(result);
    mock.Verify(x => x.GetByEmail("test@example.com")).Once();
}
```

---

## Full Mock vs Partial Mock

| Scenario | Use |
|----------|-----|
| Interface with 2-3 methods | `Mock.Of<T>()` |
| Interface with many methods, testing few | `Mock.Partial<T>()` |
| Need all methods to work | `Mock.Of<T>()` with all setups |

```csharp
// Full mock - Setup ALL methods you call
var full = Mock.Of<IOrderService>()
    .Setup(x => x.GetById(1), order)
    .Setup(x => x.CalculateTotalAsync(1), Task.FromResult(100m))
    .Build();

// Partial mock - Setup ONLY what you need
var partial = Mock.Partial<IOrderService>()
    .Only(x => x.GetById(1), order)
    .Build();
// CalculateTotalAsync will throw NotImplementedException if called
```
