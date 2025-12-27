using System.Runtime.CompilerServices;
using NimbleMock.Internal;

namespace NimbleMock;

/// <summary>
/// Provides static methods for creating mock instances.
/// </summary>
public static class Mock
{
    /// <summary>
    /// Creates a new mock builder for the specified interface type.
    /// </summary>
    /// <typeparam name="T">The interface type to mock.</typeparam>
    /// <returns>A mock builder that can be configured with method setups.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MockBuilder<T> Of<T>() where T : class
        => new(new MethodSetup[32]);
    
    /// <summary>
    /// Creates a partial mock builder for the specified interface type.
    /// Only specified methods will be mocked; others will throw NotImplementedException.
    /// </summary>
    /// <typeparam name="T">The interface type to partially mock.</typeparam>
    /// <returns>A partial mock builder that can be configured with method setups.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PartialMock<T> Partial<T>() where T : class
        => new(new MethodSetup[8]);
}
