using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace TailwindVariants.NET.SourceGenerator;

/// <summary>
/// Generates source code to provide strongly-typed accessors and extension methods for slot-based mapping types at
/// compile time.
/// </summary>
/// <remarks>This generator scans the compilation for types that use slot mapping patterns and produces helper
/// classes, enums, and extension methods to simplify and optimize access to slot properties. The generated code enables
/// type-safe access to slot names and values, reducing reliance on string-based lookups and improving maintainability.
/// This generator is intended for use with types following the SlotMap&lt;T&gt; convention and is typically invoked
/// automatically by the build process when included in a project.</remarks>
[Generator]
public class SlotsAccessorGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is TypeDeclarationSyntax,
                static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static s => s is not null)
            .Select(static (symbol, _) => symbol);

        context.RegisterSourceOutput(candidates, GenerateForSlotsType);
    }

    private static void GenerateForSlotsType(SourceProductionContext spc, SlotsAccessorToGenerate? gen)
    {
        if (gen is not SlotsAccessorToGenerate accessor)
        {
            return;
        }

        if (!IsPartial(accessor, out var diag))
        {
            spc.ReportDiagnostic(diag);
            return;
        }

        if (!HasProperties(accessor, out diag))
        {
            spc.ReportDiagnostic(diag);
            return;
        }

        var slotsMapName = $"SlotsMap<{accessor.FullName}>";

        var enumName = SymbolHelper.MakeSafeIdentifier($"{accessor.TypeName}SlotsTypes");
        var extClassName = SymbolHelper.MakeSafeIdentifier($"{accessor.TypeName}SlotsExtensions");
        var namesClass = SymbolHelper.MakeSafeIdentifier($"{accessor.TypeName}SlotsNames");

        var filename = SymbolHelper.MakeSafeFileName($"{accessor.FullName}.g.cs");

        var sb = new Indenter();
        WritePreamble(sb, accessor.NamespaceName);

        WriteNestedOpenings(sb, accessor.Hierarchy);
        WriteISlotsClass(sb, accessor.Name, accessor.Modifiers, accessor.Properties);
        WriteNestedClosings(sb, accessor.Hierarchy);

        WriteEnum(sb, enumName, accessor.Properties);
        WriteNamesHelper(sb, namesClass, enumName, accessor.Properties);
        WriteExtensions(sb, extClassName, slotsMapName, enumName, namesClass, accessor.Properties);

        spc.AddSource(filename, SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static SlotsAccessorToGenerate? GetSemanticTargetForGeneration(GeneratorSyntaxContext ctx)
    {
        if (ctx.SemanticModel.GetDeclaredSymbol(ctx.Node) is not INamedTypeSymbol symbol || !ImplementsISlots(symbol))
        {
            return null;
        }

        return new SlotsAccessorToGenerate(
            Name: symbol.Name,
            FullName: symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", ""),
            TypeName: symbol.ContainingType?.Name ?? symbol.Name.Replace("Slots", string.Empty),
            NamespaceName: symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
            Modifiers: GetSlotsModifiers(symbol),
            IsPartial: symbol.DeclaringSyntaxReferences
                .Any(sr => sr.GetSyntax() is TypeDeclarationSyntax tds &&
                           tds.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword))),
            Hierarchy: GetSlotsHierarchy(symbol),
            Properties: CollectPublicProperties(symbol))
        {
            Location = symbol.Locations.FirstOrDefault()
        };
    }

    #region Helpers

    private static ImmutableArray<string> CollectPublicProperties(INamedTypeSymbol type) =>
        [.. type.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => !p.IsStatic && p.DeclaredAccessibility == Accessibility.Public)
            .OrderBy(p => p.Locations.FirstOrDefault()?.SourceSpan.Start ?? int.MaxValue)
            .ThenBy(p => p.Name, StringComparer.Ordinal)
            .Select(x => x.Name)];

    private static ImmutableArray<string> GetSlotsHierarchy(INamedTypeSymbol slotsType)
    {
        var typeStack = new Stack<string>();
        var current = slotsType;
        while (current != null)
        {
            typeStack.Push(current.Name);
            current = current.ContainingType;
        }
        return [.. typeStack];
    }

    private static string GetSlotsModifiers(INamedTypeSymbol slotsType)
    {
        var mods = slotsType.DeclaredAccessibility switch
        {
            Accessibility.Public => "public ",
            Accessibility.Internal => "internal ",
            _ => ""
        };
        if (slotsType.IsSealed) mods += "sealed ";
        else if (slotsType.IsAbstract) mods += "abstract ";

        mods += "partial class";
        return mods;
    }

    private static bool HasProperties(SlotsAccessorToGenerate accessor, out Diagnostic diag)
    {
        if (accessor.Properties.Length == 0)
        {
            diag = Diagnostic.Create(
               DiagnosticHelper.NoPropertiesDescriptor,
               accessor.Location,
               accessor.Name);
            return false;
        }

        diag = null!;
        return true;
    }

    private static bool ImplementsISlots(INamedTypeSymbol type)
        => type.AllInterfaces.Any(i => i.ToDisplayString() == "TailwindVariants.NET.ISlots");

    private static bool IsPartial(SlotsAccessorToGenerate accessor, out Diagnostic diag)
    {
        if (!accessor.IsPartial)
        {
            diag = Diagnostic.Create(
                DiagnosticHelper.MustBePartial,
                accessor.Location,
                accessor.Name);
            return false;
        }

        diag = null!;
        return true;
    }

    #endregion Helpers

    #region Code Writing

    private static void WriteEnum(Indenter sb, string enumName, ImmutableArray<string> properties)
    {
        sb.AppendLine($"public enum {enumName}");
        sb.AppendLine("{");
        sb.Indent();
        for (int i = 0; i < properties.Length; i++)
        {
            var nm = SymbolHelper.MakeSafeIdentifier(properties[i]);
            sb.AppendLine($"{nm} = {i},");
        }
        sb.Dedent();
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static void WriteExtensions(
        Indenter sb,
        string extClassName,
        string slotsMapName,
        string enumName,
        string namesClass,
        ImmutableArray<string> properties)
    {
        sb.AppendLine($"public static class {extClassName}");
        sb.AppendLine("{");
        sb.Indent();
        sb.AppendLine($"public static string? Get(this {slotsMapName} slots, {enumName} key) => slots.Map[{namesClass}.NameOf(key)];");
        sb.Dedent();

        sb.Indent();
        foreach (var property in properties)
        {
            var safe = SymbolHelper.MakeSafeIdentifier(property);
            sb.AppendLine();
            sb.AppendLine($"public static string? Get{safe}(this {slotsMapName} slots) => slots.Get({enumName}.{safe});");
        }
        sb.Dedent();

        sb.AppendLine("}");
    }

    private static void WriteISlotsClass(Indenter sb, string typeName, string mods, ImmutableArray<string> properties)
    {
        sb.AppendLine($"{mods} {typeName}");
        sb.AppendLine("{");
        sb.Indent();

        sb.AppendLine("public IEnumerable<(string Slot, string Value)> EnumerateOverrides()");
        sb.AppendLine("{");
        sb.Indent();

        foreach (var property in properties)
        {
            sb.AppendLine($"if (!string.IsNullOrWhiteSpace({property}))");
            sb.Indent();
            sb.AppendLine($"yield return (nameof({property}), {property}!);");
            sb.Dedent();
        }

        sb.Dedent();
        sb.AppendLine("}");

        sb.Dedent();
        sb.AppendLine("}");
    }

    private static void WriteNamesHelper(Indenter sb, string namesClass, string enumName, ImmutableArray<string> properties)
    {
        sb.AppendLine($"public static class {namesClass}");
        sb.AppendLine("{");
        sb.Indent();
        sb.AppendLine($"private static readonly string[] _names = new[] {{ {string.Join(", ", properties.Select(n => "\"" + n + "\""))} }};");
        sb.AppendLine();
        sb.AppendLine($"public static IReadOnlyList<string> AllNames => _names;");
        sb.AppendLine();
        sb.AppendLine($"public static string NameOf({enumName} key) => _names[(int)key];");
        sb.Dedent();
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static void WriteNestedClosings(Indenter sb, ImmutableArray<string> hierarchy)
    {
        foreach (var _ in hierarchy.Take(hierarchy.Length - 1))
        {
            sb.Dedent();
            sb.AppendLine("}");
        }

        sb.AppendLine();
    }

    private static void WriteNestedOpenings(Indenter sb, ImmutableArray<string> hierarchy)
    {
        foreach (var container in hierarchy.Take(hierarchy.Length - 1))
        {
            sb.AppendLine($"partial class {container}");
            sb.AppendLine("{");
            sb.Indent();
        }
    }

    private static void WritePreamble(Indenter sb, string namespaceName)
    {
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("using TailwindVariants.NET;");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        if (!string.IsNullOrEmpty(namespaceName))
        {
            sb.AppendLine($"namespace {namespaceName};");
            sb.AppendLine();
        }
    }

    #endregion Code Writing

    private readonly record struct SlotsAccessorToGenerate(
        string Name,
        string FullName,
        string TypeName,
        string NamespaceName,
        string Modifiers,
        bool IsPartial,
        ImmutableArray<string> Hierarchy,
        ImmutableArray<string> Properties)
    {
        public Location? Location { get; init; }
    };
}