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
                static (ctx, _) => ctx.SemanticModel.GetDeclaredSymbol(ctx.Node) as INamedTypeSymbol)
            .Where(static s => s is not null && ImplementsISlots(s))
            .Select(static (symbol, _) => symbol);

        context.RegisterSourceOutput(candidates, GenerateForSlotsType);
    }

    private static void GenerateForSlotsType(SourceProductionContext spc, INamedTypeSymbol? slotsType)
    {
        if (slotsType == null)
        {
            return;
        }

        if (!IsPartial(slotsType, out var diag))
        {
            spc.ReportDiagnostic(diag);
            return;
        }

        if (!HasProperties(slotsType, out var properties, out diag))
        {
            spc.ReportDiagnostic(diag);
            return;
        }

        var ordered = OrderProperties(properties);
        var hierarchy = GetSlotsHierarchy(slotsType);
        var mods = GetSlotsModifiers(slotsType);

        var slotNames = ordered.Select(p => p.Name).ToArray();
        var slotsTypeFull = slotsType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
        var slotMapTypeFull = $"SlotsMap<{slotsTypeFull}>";
        var slotsTypeName = slotsType.ContainingType?.Name ?? slotsType.Name.Replace("Slots", string.Empty);

        var enumName = SymbolHelper.MakeSafeIdentifier($"{slotsTypeName.Replace("Slots", string.Empty)}SlotsTypes");
        var extClassName = SymbolHelper.MakeSafeIdentifier($"{slotsTypeName.Replace("Slots", string.Empty)}SlotsExtensions");
        var namesClass = SymbolHelper.MakeSafeIdentifier($"{slotsTypeName.Replace("Slots", string.Empty)}SlotsNames");

        var namespaceName = slotsType.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        var filename = SymbolHelper.MakeSafeFileName($"{slotsType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.g.cs");

        var sb = new Indenter();
        WritePreamble(sb, namespaceName);

        WriteNestedOpenings(sb, hierarchy);
        WriteISlotsClass(sb, slotsType.Name, mods, ordered);
        WriteNestedClosings(sb, hierarchy);

        WriteEnum(sb, enumName, slotNames);
        WriteNamesHelper(sb, namesClass, enumName, slotNames);
        WriteExtensions(sb, extClassName, slotMapTypeFull, enumName, namesClass, ordered);

        spc.AddSource(filename, SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    #region Helpers

    private static IPropertySymbol[] CollectPublicProperties(INamedTypeSymbol type) =>
        [.. type.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => !p.IsStatic && p.DeclaredAccessibility == Accessibility.Public)];

    private static string[] GetSlotsHierarchy(INamedTypeSymbol slotsType)
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

    private static bool HasProperties(INamedTypeSymbol type, out IPropertySymbol[] properties, out Diagnostic diag)
    {
        properties = CollectPublicProperties(type);
        if (properties.Length == 0)
        {
            diag = Diagnostic.Create(
               DiagnosticHelper.NoPropertiesDescriptor,
               type.Locations.FirstOrDefault(),
               type.Name);
            return false;
        }

        diag = null!;
        return true;
    }

    private static bool ImplementsISlots(INamedTypeSymbol type)
            => type.AllInterfaces.Any(i => i.ToDisplayString() == "TailwindVariants.NET.ISlots");

    private static bool IsPartial(INamedTypeSymbol type, out Diagnostic diag)
    {
        var isPartial = type.DeclaringSyntaxReferences.Any(sr =>
            sr.GetSyntax() is TypeDeclarationSyntax tds &&
            tds.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)));

        if (!isPartial)
        {
            diag = Diagnostic.Create(
                DiagnosticHelper.MustBePartial,
                type.Locations.FirstOrDefault(),
                type.Name);
            return false;
        }

        diag = null!;
        return true;
    }

    private static IPropertySymbol[] OrderProperties(IPropertySymbol[] properties) =>
        [.. properties.OrderBy(p => p.Locations.FirstOrDefault()?.SourceSpan.Start ?? int.MaxValue)
            .ThenBy(p => p.Name, StringComparer.Ordinal)];

    #endregion Helpers

    #region Code Writing

    private static void WriteEnum(Indenter sb, string enumName, string[] slotNames)
    {
        sb.AppendLine($"public enum {enumName}");
        sb.AppendLine("{");
        sb.Indent();
        for (int i = 0; i < slotNames.Length; i++)
        {
            var nm = SymbolHelper.MakeSafeIdentifier(slotNames[i]);
            sb.AppendLine($"{nm} = {i},");
        }
        sb.Dedent();
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static void WriteExtensions(
        Indenter sb,
        string extClassName,
        string slotMapTypeFull,
        string enumName,
        string namesClass,
        IPropertySymbol[] props)
    {
        sb.AppendLine($"public static class {extClassName}");
        sb.AppendLine("{");
        sb.Indent();
        sb.AppendLine($"public static string? Get(this {slotMapTypeFull} slots, {enumName} key) => slots.Map[{namesClass}.NameOf(key)];");
        sb.Dedent();

        sb.Indent();
        foreach (var p in props)
        {
            var safe = SymbolHelper.MakeSafeIdentifier(p.Name);
            sb.AppendLine();
            sb.AppendLine($"public static string? Get{safe}(this {slotMapTypeFull} slots) => slots.Get({enumName}.{safe});");
        }
        sb.Dedent();

        sb.AppendLine("}");
    }

    private static void WriteISlotsClass(Indenter sb, string typeName, string mods, IPropertySymbol[] props)
    {
        sb.AppendLine($"{mods} {typeName}");
        sb.AppendLine("{");
        sb.Indent();

        sb.AppendLine("public IEnumerable<(string Slot, string Value)> EnumerateOverrides()");
        sb.AppendLine("{");
        sb.Indent();

        foreach (var p in props)
        {
            sb.AppendLine($"if (!string.IsNullOrWhiteSpace({p.Name}))");
            sb.Indent();
            sb.AppendLine($"yield return (nameof({p.Name}), {p.Name}!);");
            sb.Dedent();
        }

        sb.Dedent();
        sb.AppendLine("}");

        sb.Dedent();
        sb.AppendLine("}");
    }

    private static void WriteNamesHelper(Indenter sb, string namesClass, string enumName, string[] slotNames)
    {
        sb.AppendLine($"public static class {namesClass}");
        sb.AppendLine("{");
        sb.Indent();
        sb.AppendLine($"private static readonly string[] _names = new[] {{ {string.Join(", ", slotNames.Select(n => "\"" + n + "\""))} }};");
        sb.AppendLine();
        sb.AppendLine($"public static IReadOnlyList<string> AllNames => _names;");
        sb.AppendLine();
        sb.AppendLine($"public static string NameOf({enumName} key) => _names[(int)key];");
        sb.Dedent();
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static void WriteNestedClosings(Indenter sb, string[] hierarchy)
    {
        foreach (var _ in hierarchy.Take(hierarchy.Length - 1))
        {
            sb.Dedent();
            sb.AppendLine("}");
        }

        sb.AppendLine();
    }

    private static void WriteNestedOpenings(Indenter sb, string[] hierarchy)
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
}
