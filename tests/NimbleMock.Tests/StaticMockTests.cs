using System;
using Xunit;
using NimbleMock;
using NimbleMock.Exceptions;

namespace NimbleMock.Tests;

/// <summary>
/// Tests for the Shim API - demonstrating the wrapper pattern for static dependencies.
/// </summary>
public class ShimTests
{
    /// <summary>
    /// Example shim interface for DateTime - this is the recommended pattern.
    /// </summary>
    public interface IDateTimeProvider
    {
        DateTime Now { get; }
        DateTime UtcNow { get; }
    }
    
    [Fact]
    public void Shim_CanCreateBuilder()
    {
        var builder = Shim.For<IDateTimeProvider>();
        var _ = builder;
        Assert.True(true);
    }
    
    [Fact]
    public void Shim_CanSetupAndBuild()
    {
        var fixedDate = new DateTime(2024, 1, 1, 12, 0, 0);
        var mock = Shim.For<IDateTimeProvider>()
            .Replace(d => d.Now, fixedDate)
            .Build();
        
        Assert.NotNull(mock);
    }
    
    [Fact]
    public void Shim_ReturnsConfiguredValue()
    {
        var fixedDate = new DateTime(2024, 6, 15, 12, 0, 0);
        var mock = Shim.For<IDateTimeProvider>()
            .Replace(d => d.Now, fixedDate)
            .Replace(d => d.UtcNow, fixedDate.ToUniversalTime())
            .Build();
        
        Assert.Equal(fixedDate, mock.Object.Now);
    }
    
    [Fact]
    public void Shim_CanVerifyCalls()
    {
        var fixedDate = new DateTime(2024, 1, 1, 12, 0, 0);
        var mock = Shim.For<IDateTimeProvider>()
            .Replace(d => d.Now, fixedDate)
            .Build();
        
        // No calls yet
        mock.Verify(d => d.Now).Never();
        
        // Make a call
        _ = mock.Object.Now;
        
        // Now verify it was called
        mock.Verify(d => d.Now).Once();
    }
    
    [Fact]
    public void MockOf_AlsoWorks_ForShimPattern()
    {
        // Shim.For<T>() is just syntax sugar, Mock.Of<T>() works the same
        var fixedDate = new DateTime(2024, 1, 1);
        var mock = Mock.Of<IDateTimeProvider>()
            .Setup(d => d.Now, fixedDate)
            .Build();
        
        Assert.Equal(fixedDate, mock.Object.Now);
    }
}
