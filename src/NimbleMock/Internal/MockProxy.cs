using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NimbleMock.Internal;

/// <summary>
/// Provides factory registration and proxy creation for mocked types.
/// This class is public to allow source-generated factories in external assemblies to register.
/// </summary>
public static partial class MockProxy<T> where T : class
{
    private static readonly ObjectPool<MockInstance<T>> Pool = new();
    
    private static Func<MockInstance<T>, T>? _factory;
    
    /// <summary>
    /// Registers a factory delegate for creating mock proxy instances.
    /// Called by source-generated code during module initialization.
    /// </summary>
    /// <param name="factory">The factory delegate that creates proxy instances.</param>
    public static void RegisterFactory(Func<MockInstance<T>, T> factory)
    {
        _factory = factory;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T CreateProxy(MockInstance<T> instance)
    {
        if (_factory != null)
            return _factory(instance);
        
        return CreateProxyReflection(instance);
    }
    
    private static T CreateProxyReflection(MockInstance<T> instance)
    {
        var typeName = $"MockProxy_{typeof(T).Name}";
        var assembly = typeof(T).Assembly;
        var proxyType = assembly.GetType(typeName) ?? 
                       AppDomain.CurrentDomain.GetAssemblies()
                           .SelectMany(a => a.GetTypes())
                           .FirstOrDefault(t => t.Name == typeName);
        
        if (proxyType == null)
        {
            throw new InvalidOperationException(
                $"Mock proxy for {typeof(T).Name} not found. Ensure NimbleMock.SourceGenerator is referenced and the type is mocked.");
        }
        
        var constructor = proxyType.GetConstructor(new[] { typeof(MockInstance<T>) });
        if (constructor == null)
        {
            throw new InvalidOperationException(
                $"Mock proxy {typeName} does not have a constructor accepting MockInstance<{typeof(T).Name}>.");
        }
        
        return (T)constructor.Invoke(new object[] { instance });
    }
    
    /// <summary>
    /// Creates a full mock with all specified setups.
    /// </summary>
    /// <param name="setups">The method setups to apply.</param>
    /// <returns>A verifiable mock instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VerifiableMock<T> Create(MethodSetup[] setups)
    {
        var instance = Pool.Rent();
        instance.Initialize(setups, isPartial: false);
        instance.Proxy = CreateProxy(instance);
        return new NimbleMock.VerifiableMock<T>(instance);
    }
    
    /// <summary>
    /// Creates a partial mock where only specified methods are mocked.
    /// </summary>
    /// <param name="setups">The method setups to apply.</param>
    /// <returns>A verifiable mock instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VerifiableMock<T> CreatePartial(MethodSetup[] setups)
    {
        var instance = Pool.Rent();
        instance.Initialize(setups, isPartial: true);
        instance.Proxy = CreateProxy(instance);
        return new NimbleMock.VerifiableMock<T>(instance);
    }
}

