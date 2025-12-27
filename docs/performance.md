# Performance Benchmarks

NimbleMock is designed for maximum performance with zero allocations in typical scenarios.

## Benchmark Results

### Mock Creation (Setup)

| Library | Time | Memory Allocated |
|---------|------|------------------|
| **Moq** | 48,812 ns | 10.37 KB |
| **NSubstitute** | 9,937 ns | 12.36 KB |
| **NimbleMock** | **1,415 ns** | **3.45 KB** |

**NimbleMock is 34× as fast as Moq and 7× as fast as NSubstitute in mock creation.**

### Method Execution

Method execution benchmarks measure the time to call a mocked method and retrieve the configured return value. These benchmarks test the overhead of invoking methods on mock instances.

| Library | Time | vs Baseline |
|---------|------|-------------|
| **Moq** | ~1.4 μs | Baseline |
| **NSubstitute** | ~1.6 μs | 14% slower |
| **NimbleMock** | **~0.6 μs** | **2.3× as fast** |

*Note: "2.3× as fast" means NimbleMock takes 1/2.3 of the time, equivalent to being 133% faster.*


### Verification

| Library | Time | Memory Allocated |
|---------|------|------------------|
| **Moq** | 1,795 ns | 2.12 KB |
| **NSubstitute** | 2,163 ns | 2.82 KB |
| **NimbleMock** | **585 ns** | **0.53 KB** |

**NimbleMock is 3× as fast as Moq and 3.7× as fast as NSubstitute in verification.**

*Benchmarks: .NET 8.0.22, x64, RyuJIT AVX2, Windows 11*

## Why NimbleMock is Faster

1. **Stack allocation** via arrays for setup buffers
2. **Source generation** creates compile-time proxies (no Castle.DynamicProxy)
3. **Object pooling** for instance reuse
4. **Direct lookups** O(1) instead of dictionary overhead
5. **Aggressive inlining** on hot paths

## Running Benchmarks

To run all benchmarks:

```bash
dotnet run --project tests/NimbleMock.Benchmarks --configuration Release --filter *
```

To run specific benchmark suites:

```bash
# Mock creation benchmarks
dotnet run --project tests/NimbleMock.Benchmarks --configuration Release --filter "*MockCreationBenchmarks*"

# Method execution benchmarks
dotnet run --project tests/NimbleMock.Benchmarks --configuration Release --filter "*MockExecutionBenchmarks*"

# Verification benchmarks
dotnet run --project tests/NimbleMock.Benchmarks --configuration Release --filter "*VerificationBenchmarks*"
```

Results are saved to `tests/NimbleMock.Benchmarks/BenchmarkDotNet.Artifacts/results/` in multiple formats (CSV, HTML, Markdown).

## Performance Tips

- Use `Mock.Partial<T>()` for large interfaces where you only mock a few methods
- Reuse mock instances when possible
- Prefer `SetupAsync` for async methods (analyzer will warn if you forget)

