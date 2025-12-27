using Microsoft.CodeAnalysis;
using System.Linq;
using System.Text;

namespace NimbleMock.SourceGenerator.Internal;

internal static class CodeGenerationHelpers
{
    /// <summary>
    /// Generates default argument values for method parameters.
    /// Uses stack-allocated spans where possible for zero-allocation.
    /// </summary>
    public static string GetDefaultArgs(IMethodSymbol method)
    {
        if (method.Parameters.Length == 0)
            return "";

        return string.Join(", ", method.Parameters.Select(p =>
            p.Type.IsValueType ? "default" : "default!"));
    }

    /// <summary>
    /// Generates a property implementation for the mock proxy.
    /// </summary>
    public static void GeneratePropertyBody(
        StringBuilder sb,
        IPropertySymbol property,
        string interfaceTypeName)
    {
        var propertyType = property.Type.ToDisplayString();
        var propertyName = property.Name;

        sb.AppendLine($"    public {propertyType} {propertyName}");
        sb.AppendLine("    {");

        if (property.GetMethod != null)
        {
            sb.AppendLine("        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine("        get");
            sb.AppendLine("        {");
            // Use expression-based MethodId for consistency with Setup
            sb.AppendLine($"            var methodId = MethodId.From<{interfaceTypeName}>(");
            sb.AppendLine($"                ({interfaceTypeName} x) => x.{propertyName});");
            sb.AppendLine("            _instance.RecordCall(methodId, Array.Empty<object?>());");
            sb.AppendLine();
            sb.AppendLine("            if (!_instance.TryGetSetup(methodId, out var setup))");
            sb.AppendLine("            {");
            sb.AppendLine("                if (_instance.IsPartial)");
            sb.AppendLine($"                    throw new NotImplementedException($\"Property {propertyName} is not mocked in partial mock.\");");
            sb.AppendLine($"                return default({propertyType})!;");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            if (setup.IsException)");
            sb.AppendLine("                throw (Exception)setup.ReturnValue!;");
            sb.AppendLine();
            sb.AppendLine($"            return ({propertyType})setup.ReturnValue!;");
            sb.AppendLine("        }");
        }

        if (property.SetMethod != null)
        {
            sb.AppendLine("        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine("        set");
            sb.AppendLine("        {");
            sb.AppendLine($"            var methodId = MethodId.FromName<{interfaceTypeName}>(\"set_{propertyName}\");");
            sb.AppendLine("            _instance.RecordCall(methodId, new object?[] { value });");
            sb.AppendLine("        }");
        }

        sb.AppendLine("    }");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates a method implementation for the mock proxy.
    /// Uses expression-based MethodId for consistency with Setup/Verify.
    /// </summary>
    public static void GenerateMethodBody(
        StringBuilder sb,
        IMethodSymbol method,
        string interfaceTypeName)
    {
        var returnType = method.ReturnType.ToDisplayString();
        var methodName = method.Name;
        var parameters = string.Join(", ", method.Parameters.Select(p =>
            $"{p.Type.ToDisplayString()} {p.Name}"));
        var paramNames = string.Join(", ", method.Parameters.Select(p => p.Name));
        var paramCount = method.Parameters.Length;

        sb.AppendLine("    [MethodImpl(MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"    public {returnType} {methodName}({parameters})");
        sb.AppendLine("    {");

        // Use expression-based MethodId for consistency with Setup/Verify
        sb.AppendLine($"        var methodId = MethodId.From<{interfaceTypeName}>(");
        sb.AppendLine($"            ({interfaceTypeName} x) => x.{methodName}({GetDefaultArgs(method)}));");
        sb.AppendLine();

        // Record call - use Array.Empty for zero-alloc when no params
        if (paramCount > 0)
        {
            sb.AppendLine($"        _instance.RecordCall(methodId, new object?[] {{ {paramNames} }});");
        }
        else
        {
            sb.AppendLine("        _instance.RecordCall(methodId, Array.Empty<object?>());");
        }
        sb.AppendLine();

        // Get setup
        sb.AppendLine("        if (!_instance.TryGetSetup(methodId, out var setup))");
        sb.AppendLine("        {");
        sb.AppendLine("            if (_instance.IsPartial)");
        sb.AppendLine($"                throw new NotImplementedException($\"Method {methodName} is not mocked in partial mock.\");");
        if (method.ReturnsVoid)
        {
            sb.AppendLine("            return;");
        }
        else
        {
            sb.AppendLine($"            return default({returnType})!;");
        }
        sb.AppendLine("        }");
        sb.AppendLine();

        // Handle exceptions
        sb.AppendLine("        if (setup.IsException)");
        sb.AppendLine("            throw (Exception)setup.ReturnValue!;");
        sb.AppendLine();

        // Return value
        if (!method.ReturnsVoid)
        {
            sb.AppendLine($"        return ({returnType})setup.ReturnValue!;");
        }

        sb.AppendLine("    }");
        sb.AppendLine();
    }
}
