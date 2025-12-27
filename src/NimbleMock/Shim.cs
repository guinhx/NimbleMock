using System;
using System.Runtime.CompilerServices;
using NimbleMock.Internal;

namespace NimbleMock;

/// <summary>
/// Provides static methods for creating shim-based wrappers for static/sealed types.
/// </summary>
public static class Shim
{
    /// <summary>
    /// Creates a shim builder for the specified type.
    /// Use this to create testable wrappers around static dependencies like DateTime.
    /// </summary>
    /// <typeparam name="T">The type to create a shim for.</typeparam>
    /// <returns>A shim builder for configuring the wrapped type.</returns>
    /// <example>
    /// <code>
    /// // 1. Define a shim interface
    /// public interface IDateTimeProvider
    /// {
    ///     DateTime Now { get; }
    ///     DateTime UtcNow { get; }
    /// }
    /// 
    /// // 2. Create a default implementation
    /// public class SystemDateTimeProvider : IDateTimeProvider
    /// {
    ///     public DateTime Now => DateTime.Now;
    ///     public DateTime UtcNow => DateTime.UtcNow;
    /// }
    /// 
    /// // 3. In tests, mock the interface
    /// var mock = Mock.Of&lt;IDateTimeProvider&gt;()
    ///     .Setup(x => x.Now, new DateTime(2024, 1, 1))
    ///     .Build();
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ShimBuilder<T> For<T>() where T : class
        => new(new MethodSetup[16]);
}

/// <summary>
/// A builder for configuring shim wrappers with Zero-allocation design.
/// </summary>
/// <typeparam name="T">The type being shimmed.</typeparam>
public ref struct ShimBuilder<T> where T : class
{
    private readonly MethodSetup[] _setups;
    private int _count;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ShimBuilder(MethodSetup[] buffer)
    {
        _setups = buffer;
        _count = 0;
    }
    
    /// <summary>
    /// Configures a member to return a specific value.
    /// </summary>
    /// <typeparam name="TResult">The return type of the member.</typeparam>
    /// <param name="expr">Expression selecting the member.</param>
    /// <param name="value">The value to return.</param>
    /// <returns>This builder for method chaining.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ShimBuilder<T> Replace<TResult>(
        System.Linq.Expressions.Expression<Func<T, TResult>> expr, 
        TResult value)
    {
        _setups[_count++] = new MethodSetup(
            MethodId.From<T>(expr),
            value!);
        return this;
    }
    
    /// <summary>
    /// Builds a mock instance of the shim interface.
    /// </summary>
    /// <returns>A verifiable mock that can be used in tests.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VerifiableMock<T> Build()
    {
        var setups = new MethodSetup[_count];
        Array.Copy(_setups, setups, _count);
        return MockProxy<T>.Create(setups);
    }
}
