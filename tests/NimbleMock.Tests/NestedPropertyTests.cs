using System;
using Xunit;
using NimbleMock;

namespace NimbleMock.Tests;

/// <summary>
/// Tests for nested property mocking feature.
/// </summary>
public class NestedPropertyTests
{
    // Test interfaces for nested property scenarios
    public interface IAppConfig
    {
        IDatabaseConfig Database { get; }
        ILoggingConfig Logging { get; }
    }

    public interface IDatabaseConfig
    {
        string ConnectionString { get; }
        int MaxConnections { get; }
    }

    public interface ILoggingConfig
    {
        string Level { get; }
        bool Enabled { get; }
    }

    // Concrete interface instead of generic IOptions<T> to avoid source generator issues with open generics
    public interface IAppSettingsOptions
    {
        AppSettings Value { get; }
    }

    public class AppSettings
    {
        public string AppName { get; set; } = "";
        public DatabaseSettings? Database { get; set; }
    }

    public class DatabaseSettings
    {
        public string Server { get; set; } = "";
        public string Name { get; set; } = "";
    }

    [Fact]
    public void SetupNested_SingleLevel_WorksLikeRegularSetup()
    {
        var mock = Mock.Of<IDatabaseConfig>()
            .SetupNested(x => x.ConnectionString, "Server=localhost")
            .Build();

        Assert.Equal("Server=localhost", mock.Object.ConnectionString);
    }

    [Fact]
    public void Setup_WithPOCO_WorksDirectly()
    {
        // For POCOs (non-interface), regular setup works
        var settings = new AppSettings 
        { 
            AppName = "TestApp",
            Database = new DatabaseSettings { Server = "localhost", Name = "testdb" }
        };

        var mock = Mock.Of<IAppSettingsOptions>()
            .Setup(x => x.Value, settings)
            .Build();

        Assert.Equal("TestApp", mock.Object.Value.AppName);
        Assert.Equal("localhost", mock.Object.Value.Database?.Server);
    }

    [Fact]
    public void Setup_NestedInterface_RequiresIntermediateMock()
    {
        // For nested interfaces, mock them separately then compose
        var dbMock = Mock.Of<IDatabaseConfig>()
            .Setup(x => x.ConnectionString, "Server=localhost")
            .Setup(x => x.MaxConnections, 100)
            .Build();

        var configMock = Mock.Of<IAppConfig>()
            .Setup(x => x.Database, dbMock.Object)
            .Build();

        Assert.Equal("Server=localhost", configMock.Object.Database.ConnectionString);
        Assert.Equal(100, configMock.Object.Database.MaxConnections);
    }

    [Fact]
    public void ExpressionHelper_ExtractsPropertyChain()
    {
        // Test the expression helper directly
        var chain = ExpressionHelper.GetPropertyChain<IAppConfig, string>(
            x => x.Database.ConnectionString);

        Assert.Equal(2, chain.Count);
        Assert.Equal("Database", chain[0].Name);
        Assert.Equal("ConnectionString", chain[1].Name);
    }
}
