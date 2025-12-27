using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using NimbleMock.SourceGenerator.Internal;

namespace NimbleMock.SourceGenerator;

[Generator]
public class MockProxyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var mockCalls = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: IsMockCall,
                transform: GetMockType)
            .Where(static m => m is not null);

        var compilation = context.CompilationProvider.Combine(mockCalls.Collect());

        context.RegisterSourceOutput(compilation, GenerateProxies!);
    }

    /// <summary>
    /// Detects Mock.Of, Mock.Partial, Mock.Static, and Shim.For calls.
    /// </summary>
    private static bool IsMockCall(SyntaxNode node, CancellationToken ct)
    {
        if (node is not InvocationExpressionSyntax invocation)
            return false;
            
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;

        var methodName = memberAccess.Name.Identifier.ValueText;
        
        // Check for Mock.Of, Mock.Partial, Mock.Static
        if (memberAccess.Expression is IdentifierNameSyntax { Identifier.ValueText: "Mock" })
        {
            return methodName is "Of" or "Partial" or "Static";
        }
        
        // Check for Shim.For
        if (memberAccess.Expression is IdentifierNameSyntax { Identifier.ValueText: "Shim" })
        {
            return methodName == "For";
        }
        
        return false;
    }

    private static INamedTypeSymbol? GetMockType(
        GeneratorSyntaxContext context,
        CancellationToken ct)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var memberAccess = (MemberAccessExpressionSyntax)invocation.Expression;
        
        if (memberAccess.Name is not GenericNameSyntax generic)
            return null;

        var typeArg = generic.TypeArgumentList.Arguments[0];
        var typeInfo = context.SemanticModel.GetTypeInfo(typeArg, ct);
        
        return typeInfo.Type as INamedTypeSymbol;
    }

    private static void GenerateProxies(
        SourceProductionContext context,
        (Compilation Left, ImmutableArray<INamedTypeSymbol> Right) source)
    {
        var (compilation, types) = source;
        var uniqueTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        
        foreach (var type in types)
        {
            if (type != null)
                uniqueTypes.Add(type);
        }

        foreach (var type in uniqueTypes)
        {
            try
            {
                var (proxySource, factorySource) = GenerateProxy(type);
                var safeFileName = GetSafeFileName(type);
                
                context.AddSource(
                    $"Mock_{safeFileName}.g.cs",
                    SourceText.From(proxySource, Encoding.UTF8));
                context.AddSource(
                    $"Mock_{safeFileName}_Factory.g.cs",
                    SourceText.From(factorySource, Encoding.UTF8));
            }
            catch (System.Exception ex)
            {
                // Report diagnostic instead of failing silently
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.ProxyGenerationFailed,
                    Location.None,
                    type.ToDisplayString(),
                    ex.Message));
            }
        }
    }

    /// <summary>
    /// Creates a safe file name that handles nested types.
    /// </summary>
    private static string GetSafeFileName(INamedTypeSymbol type)
    {
        var parts = new List<string>();
        var current = type;
        
        while (current != null)
        {
            parts.Insert(0, current.Name);
            current = current.ContainingType;
        }
        
        return string.Join("_", parts);
    }

    /// <summary>
    /// Gets the full type name including containing types for nested types.
    /// </summary>
    private static string GetFullTypeName(INamedTypeSymbol type)
    {
        var ns = type.ContainingNamespace?.ToDisplayString();
        var isGlobalNamespace = string.IsNullOrEmpty(ns) || ns == "<global namespace>";
        
        // Build the type name chain for nested types
        var typeChain = new List<string>();
        var current = type;
        while (current != null)
        {
            typeChain.Insert(0, current.Name);
            current = current.ContainingType;
        }
        
        var typePath = string.Join(".", typeChain);
        
        return isGlobalNamespace ? typePath : $"{ns}.{typePath}";
    }

    /// <summary>
    /// Gets a unique proxy name that handles nested types.
    /// </summary>
    private static string GetProxyName(INamedTypeSymbol type)
    {
        var parts = new List<string>();
        var current = type;
        
        while (current != null)
        {
            parts.Insert(0, current.Name);
            current = current.ContainingType;
        }
        
        return $"MockProxy_{string.Join("_", parts)}";
    }

    private static (string ProxySource, string FactorySource) GenerateProxy(INamedTypeSymbol interfaceType)
    {
        var fullTypeName = GetFullTypeName(interfaceType);
        var proxyName = GetProxyName(interfaceType);
        var interfaceNs = interfaceType.ContainingNamespace?.ToDisplayString();
        var isGlobalNamespace = string.IsNullOrEmpty(interfaceNs) || interfaceNs == "<global namespace>";
        
        // For nested types, we need to determine the outermost namespace
        var effectiveNamespace = isGlobalNamespace ? "NimbleMock.Generated" : interfaceNs;

        var proxySb = new StringBuilder();
        proxySb.AppendLine("// <auto-generated/>");
        proxySb.AppendLine("#nullable enable");
        proxySb.AppendLine("using System;");
        proxySb.AppendLine("using System.Runtime.CompilerServices;");
        proxySb.AppendLine("using NimbleMock.Internal;");
        proxySb.AppendLine();
        proxySb.AppendLine($"namespace {effectiveNamespace};");
        proxySb.AppendLine();
        proxySb.AppendLine($"internal sealed class {proxyName} : {fullTypeName}");
        proxySb.AppendLine("{");
        proxySb.AppendLine($"    private readonly MockInstance<{fullTypeName}> _instance;");
        proxySb.AppendLine();
        proxySb.AppendLine($"    public {proxyName}(MockInstance<{fullTypeName}> instance)");
        proxySb.AppendLine("        => _instance = instance;");
        proxySb.AppendLine();

        // Generate properties first
        var properties = interfaceType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => !p.IsIndexer)
            .ToArray();

        foreach (var property in properties)
        {
            CodeGenerationHelpers.GeneratePropertyBody(proxySb, property, fullTypeName);
        }

        // Generate methods
        var methods = interfaceType.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.MethodKind == MethodKind.Ordinary)
            .ToArray();

        foreach (var method in methods)
        {
            CodeGenerationHelpers.GenerateMethodBody(proxySb, method, fullTypeName);
        }

        proxySb.AppendLine("}");

        // Generate factory - always use fully qualified type name
        var factorySb = new StringBuilder();
        factorySb.AppendLine("// <auto-generated/>");
        factorySb.AppendLine("#nullable enable");
        factorySb.AppendLine("using System;");
        factorySb.AppendLine("using System.Runtime.CompilerServices;");
        factorySb.AppendLine("using NimbleMock.Internal;");
        factorySb.AppendLine();
        factorySb.AppendLine("namespace NimbleMock.Internal;");
        factorySb.AppendLine();
        
        // Use a unique factory class name that includes nested type info
        var factoryClassName = $"MockProxy_{GetSafeFileName(interfaceType)}_Factory";
        factorySb.AppendLine($"internal static class {factoryClassName}");
        factorySb.AppendLine("{");
        factorySb.AppendLine("    [ModuleInitializer]");
        factorySb.AppendLine("    internal static void Initialize()");
        factorySb.AppendLine("    {");
        
        var proxyTypeRef = $"{effectiveNamespace}.{proxyName}";
        // Always use fully qualified type name for registration
        factorySb.AppendLine($"        MockProxy<{fullTypeName}>.RegisterFactory(instance => new {proxyTypeRef}(instance));");
        factorySb.AppendLine("    }");
        factorySb.AppendLine("}");

        return (proxySb.ToString(), factorySb.ToString());
    }

    /// <summary>
    /// Diagnostic descriptors for source generator.
    /// </summary>
    private static class Diagnostics
    {
        public static readonly DiagnosticDescriptor ProxyGenerationFailed = new(
            id: "NMOCK100",
            title: "Mock proxy generation failed",
            messageFormat: "Failed to generate mock proxy for '{0}': {1}",
            category: "NimbleMock.SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "The source generator encountered an error while generating a mock proxy.");
    }
}
