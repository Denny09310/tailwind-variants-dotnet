using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;
using TailwindVariants.NET.SourceGenerators.Helpers;

namespace TailwindVariants.NET.SourceGenerators;

[Generator]
public class SlotsAccessorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
            "SlotAttribute.g.cs",
            SourceText.From(SourceGenerationHelper.Attribute, Encoding.UTF8)));

        var candidates = context.SyntaxProvider
           .CreateSyntaxProvider(
               static (node, _) => node is TypeDeclarationSyntax,
               static (ctx, _) => GetSemanticTargetForGeneration(ctx))
           .Where(static s => s is not null)
           .Select(static (symbol, _) => symbol!.Value);

        context.RegisterSourceOutput(candidates, static (spc, accessor) => GenerateForSlotsType(spc, accessor));
    }

    private static void GenerateForSlotsType(SourceProductionContext spc, SlotsAccessorToGenerate accessor)
    {
        if (!accessor.IsPartial)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                DiagnosticHelper.MustBePartial,
                accessor.Location,
                accessor.Name));
            return;
        }

        if (accessor.Properties.IsEmpty)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
               DiagnosticHelper.NoPropertiesDescriptor,
               accessor.Location,
               accessor.Name));
            return;
        }

        var filename = SymbolHelper.MakeSafeFileName($"{accessor.FullName}.g.cs");

        var sb = new Indenter();
        WritePreamble(sb, accessor);
        WriteNestedOpenings(sb, accessor);
        WriteISlotsClass(sb, accessor);
        WriteNestedClosings(sb, accessor);
        WriteEnum(sb, accessor);
        WriteNamesHelper(sb, accessor);
        WriteExtensions(sb, accessor);
        WritePragmaClosing(sb);

        spc.AddSource(filename, SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static SlotsAccessorToGenerate? GetSemanticTargetForGeneration(GeneratorSyntaxContext ctx)
    {
        if (ctx.SemanticModel.GetDeclaredSymbol(ctx.Node) is not INamedTypeSymbol symbol || !ImplementsISlots(symbol))
        {
            return null;
        }

        // Check if this class directly implements ISlots or inherits it from a base class
        bool directlyImplementsISlots = DirectlyImplementsISlots(symbol);

        // Get the base class name if it implements ISlots
        string? baseClassName = null;
        if (!directlyImplementsISlots && symbol.BaseType != null)
        {
            baseClassName = symbol.BaseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
        }

        // collect property identifiers and effective slot names (attribute or fallback).
        var (properties, slotNames) = CollectSlotProperties(symbol);

        var fullName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
        var typeName = symbol.ContainingType?.Name ?? symbol.Name.Replace("Slots", string.Empty);

        return new SlotsAccessorToGenerate(
            Name: symbol.Name,
            FullName: fullName,
            TypeName: typeName,
            NamespaceName: symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
            Modifiers: GetSlotsModifiers(symbol),
            BaseClassName: baseClassName,
            IsPartial: symbol.DeclaringSyntaxReferences
                .Any(sr => sr.GetSyntax() is TypeDeclarationSyntax tds &&
                           tds.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword))),
            DirectlyImplementsISlots: directlyImplementsISlots,
            Hierarchy: GetSlotsHierarchy(symbol),
            Properties: properties,
            Slots: slotNames,
            SlotsMapName: $"SlotsMap<{fullName}>",
            EnumName: SymbolHelper.MakeSafeIdentifier($"{typeName}SlotsTypes"),
            ExtClassName: SymbolHelper.MakeSafeIdentifier($"{typeName}SlotsExtensions"),
            NamesClass: SymbolHelper.MakeSafeIdentifier($"{typeName}SlotsNames"),
            IsSealed: symbol.IsSealed)
        {
            Location = symbol.Locations.FirstOrDefault()
        };
    }

    #region Helpers

    private static (ImmutableArray<string> properties, ImmutableArray<string> slotNames) CollectSlotProperties(INamedTypeSymbol type)
    {
        var properties = type.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => !p.IsStatic && p.DeclaredAccessibility == Accessibility.Public && p.Type.SpecialType == SpecialType.System_String)
            .OrderBy(p => p.Locations.FirstOrDefault()?.SourceSpan.Start ?? int.MaxValue)
            .ThenBy(p => p.Name, StringComparer.Ordinal)
            .ToList();

        var propertyNamesBuilder = ImmutableArray.CreateBuilder<string>(properties.Count);
        var slotNamesBuilder = ImmutableArray.CreateBuilder<string>(properties.Count);

        foreach (var p in properties)
        {
            propertyNamesBuilder.Add(p.Name);

            string slotName = p.Name;
            var attr = p.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "TailwindVariants.NET.SlotAttribute" ||
                                     a.AttributeClass?.Name == "SlotAttribute");

            if (attr != null && attr.ConstructorArguments.Length > 0)
            {
                var arg = attr.ConstructorArguments[0];
                if (arg.Value is string s && !string.IsNullOrEmpty(s))
                {
                    slotName = s;
                }
            }

            slotNamesBuilder.Add(slotName);
        }

        return (propertyNamesBuilder.ToImmutable(), slotNamesBuilder.ToImmutable());
    }

    /// <summary>
    /// Determines if the type directly implements ISlots (not inherited from base class).
    /// </summary>
    private static bool DirectlyImplementsISlots(INamedTypeSymbol type)
    {
        // Check if ISlots is in the type's direct interfaces
        bool hasDirectInterface = type.Interfaces.Any(i => i.ToDisplayString() == "TailwindVariants.NET.ISlots");

        if (hasDirectInterface)
            return true;

        // If not directly on interfaces, check if base class implements ISlots
        if (type.BaseType != null &&
            type.BaseType.SpecialType != SpecialType.System_Object)
        {
            // Base class exists and implements ISlots, so this type doesn't directly implement it
            return !ImplementsISlots(type.BaseType);
        }

        return false;
    }

    private static ImmutableArray<string> GetSlotsHierarchy(INamedTypeSymbol slotsType)
    {
        var types = new Stack<string>();
        var current = slotsType;
        while (current != null)
        {
            types.Push(current.Name);
            current = current.ContainingType;
        }
        return [.. types];
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

    private static bool ImplementsISlots(INamedTypeSymbol type)
        => type.AllInterfaces.Any(i => i.ToDisplayString() == "TailwindVariants.NET.ISlots");
    
    #endregion Helpers

    #region Code Writing

    private static void WriteEnum(Indenter sb, SlotsAccessorToGenerate accessor)
    {
        sb.AppendLine($"public enum {accessor.EnumName}");
        sb.AppendLine("{");
        sb.Indent();
        ImmutableArray<string> properties = accessor.Properties;
        for (int i = 0; i < properties.Length; i++)
        {
            var nm = SymbolHelper.MakeSafeIdentifier(accessor.Properties[i]);
            sb.AppendLine($"{nm} = {i},");
        }
        sb.Dedent();
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static void WriteExtensions(Indenter sb, SlotsAccessorToGenerate accessor)
    {
        sb.AppendMultiline($$"""
            /// <summary>
            /// Provides extension methods for strongly-typed access to <see cref="{{accessor.FullName}}"/> 
            /// via a <see cref="SlotsMap{T}"/>.
            /// </summary>
            """);

        sb.AppendLine($"public static class {accessor.ExtClassName}");
        sb.AppendLine("{");
        sb.Indent();

        sb.AppendMultiline($$"""
            /// <summary>
            /// Gets the name of the slot identified by the specified <see cref="{{accessor.NamesClass}}"/> constant.
            /// </summary>
            /// <param name="slots">The <see cref="SlotsMap{T}"/> instance containing slot values.</param>
            /// <param name="key">The constant key representing the slot to retrieve.</param>
            /// <returns>The name of the slot.</returns>
            """);

        sb.AppendLine($"public static string GetName(this {accessor.SlotsMapName} slots, string key)");
        sb.Indent();
        sb.AppendLine($"=> {accessor.FullName}.GetName(key);");
        sb.Dedent();
        sb.AppendLine();

        sb.AppendMultiline($$"""
            /// <summary>
            /// Gets the name of the slot identified by the specified <see cref="{{accessor.EnumName}}"/> key.
            /// </summary>
            /// <param name="slots">The <see cref="SlotsMap{T}"/> instance containing slot values.</param>
            /// <param name="key">The enum key representing the slot to retrieve.</param>
            /// <returns>The name of the slot.</returns>
            """);

        sb.AppendLine($"public static string GetName(this {accessor.SlotsMapName} slots, {accessor.EnumName} key)");
        sb.Indent();
        sb.AppendLine($"=> {accessor.FullName}.GetName({accessor.NamesClass}.NameOf(key));");
        sb.Dedent();
        sb.AppendLine();

        sb.AppendMultiline($$"""
            /// <summary>
            /// Gets the value of the slot identified by the specified <see cref="{{accessor.EnumName}}"/> key.
            /// </summary>
            /// <param name="slots">The <see cref="SlotsMap{T}"/> instance containing slot values.</param>
            /// <param name="key">The enum key representing the slot to retrieve.</param>
            /// <returns>The value of the slot, or <c>null</c> if not set.</returns>
            """);

        sb.AppendLine($"public static string? Get(this {accessor.SlotsMapName} slots, {accessor.EnumName} key)");
        sb.Indent();
        sb.AppendLine($" => slots[{accessor.NamesClass}.NameOf(key)];");
        sb.Dedent();

        foreach (var property in accessor.Properties)
        {
            var safe = SymbolHelper.MakeSafeIdentifier(property);
            sb.AppendLine();
            sb.AppendMultiline($$"""
            /// <summary>
            /// Gets the value of the <c>{{property}}</c> slot.
            /// </summary>
            /// <param name="slots">The <see cref="SlotsMap{T}"/> instance containing slot values.</param>
            /// <returns>The value of the <c>{{property}}</c> slot, or <c>null</c> if not set.</returns>
            """);
            sb.AppendLine($"public static string? Get{safe}(this {accessor.SlotsMapName} slots)");
            sb.Indent();
            sb.AppendLine($" => slots.Get({accessor.EnumName}.{safe});");
            sb.Dedent();
        }

        sb.Dedent();
        sb.AppendLine("}");
    }

    private static void WriteISlotsClass(Indenter sb, SlotsAccessorToGenerate accessor)
    {
        sb.AppendLine($"{accessor.Modifiers} {accessor.Name}");
        sb.AppendLine("{");
        sb.Indent();

        // Determine if methods should be virtual or override
        // Sealed classes cannot have virtual methods
        string methodModifier = accessor.DirectlyImplementsISlots
            ? (accessor.IsSealed ? "public" : "public virtual")
            : "public override";

        sb.AppendLine($"{methodModifier} IEnumerable<(string Slot, string Value)> EnumerateOverrides()");
        sb.AppendLine("{");
        sb.Indent();

        // If overriding, call base implementation first
        if (!accessor.DirectlyImplementsISlots)
        {
            sb.AppendLine("foreach (var item in base.EnumerateOverrides())");
            sb.Indent();
            sb.AppendLine("yield return item;");
            sb.Dedent();
            sb.AppendLine();
        }

        foreach (var property in accessor.Properties)
        {
            sb.AppendLine($"if (!string.IsNullOrWhiteSpace({property}))");
            sb.Indent();
            sb.AppendLine($"yield return (GetName(nameof({property})), {property}!);");
            sb.Dedent();
        }

        sb.Dedent();
        sb.AppendLine("}");

        sb.AppendLine();
        sb.AppendLine($"public static string GetName(string propertyName)");
        sb.AppendLine("{");
        sb.Indent();

        if (accessor.DirectlyImplementsISlots)
        {
            // Direct implementation - use switch expression
            sb.AppendLine("return propertyName switch");
            sb.AppendLine("{");
            sb.Indent();
            foreach (var (property, slot) in accessor.Properties.Zip(accessor.Slots, (x, y) => (x, y)))
            {
                sb.AppendLine($"nameof({property}) => {SymbolHelper.QuoteLiteral(slot)},");
            }
            sb.AppendLine("_ => propertyName");
            sb.Dedent();
            sb.AppendLine("};");
        }
        else
        {
            // Inherited implementation - check this class first, then call base class
            sb.AppendLine("return propertyName switch");
            sb.AppendLine("{");
            sb.Indent();
            foreach (var (property, slot) in accessor.Properties.Zip(accessor.Slots, (x, y) => (x, y)))
            {
                sb.AppendLine($"nameof({property}) => {SymbolHelper.QuoteLiteral(slot)},");
            }
            sb.AppendLine($"_ => {accessor.BaseClassName}.GetName(propertyName)");
            sb.Dedent();
            sb.AppendLine("};");
        }

        sb.Dedent();
        sb.AppendLine("}");

        sb.Dedent();
        sb.AppendLine("}");
    }

    private static void WriteNamesHelper(Indenter sb, SlotsAccessorToGenerate accessor)
    {
        sb.AppendLine($"public static class {accessor.NamesClass}");
        sb.AppendLine("{");
        sb.Indent();

        foreach (var (property, slot) in accessor.Properties.Zip(accessor.Slots, (x, y) => (x, y)))
        {
            sb.AppendLine("/// <summary>");
            sb.AppendLine($"/// The slot name for <c>{property}</c>.");
            sb.AppendLine("/// </summary>");
            sb.AppendLine($"public const string {SymbolHelper.MakeSafeIdentifier(property)} = {SymbolHelper.QuoteLiteral(slot)};");
            sb.AppendLine();
        }

        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Array of slot names in the same order as the generated enum.");
        sb.AppendLine("/// </summary>");

        sb.AppendLine($"private static readonly string[] _names = new[]");
        sb.AppendLine("{");
        sb.Indent();
        foreach (var property in accessor.Properties)
        {
            sb.AppendLine($"nameof({property}),");
        }
        sb.Dedent();
        sb.AppendLine("};");

        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// All slot names (read-only).");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public static IReadOnlyList<string> AllNames => _names;");
        sb.AppendLine();
        sb.AppendLine($"/// <summary>Returns the slot name for the given {accessor.EnumName} key.</summary>");
        sb.AppendLine($"public static string NameOf({accessor.EnumName} key) => _names[(int)key];");

        sb.Dedent();
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static void WriteNestedClosings(Indenter sb, SlotsAccessorToGenerate accessor)
    {
        ImmutableArray<string> hierarchy = accessor.Hierarchy;
        foreach (var _ in accessor.Hierarchy.Take(hierarchy.Length - 1))
        {
            sb.Dedent();
            sb.AppendLine("}");
        }

        sb.AppendLine();
    }

    private static void WriteNestedOpenings(Indenter sb, SlotsAccessorToGenerate accessor)
    {
        ImmutableArray<string> hierarchy = accessor.Hierarchy;
        foreach (var container in accessor.Hierarchy.Take(hierarchy.Length - 1))
        {
            sb.AppendLine($"partial class {container}");
            sb.AppendLine("{");
            sb.Indent();
        }
    }

    private static void WritePragmaClosing(Indenter sb)
    {
        sb.AppendLine();
        sb.AppendLine("#pragma warning restore CS1591");
        sb.AppendLine("#pragma warning restore CS8618");
    }

    private static void WritePreamble(Indenter sb, SlotsAccessorToGenerate accessor)
    {
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine();
        sb.AppendLine("#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member");
        sb.AppendLine("#pragma warning disable CS8618 // Non-nullable field is uninitialized");
        sb.AppendLine();
        sb.AppendLine("using TailwindVariants.NET;");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        if (!string.IsNullOrEmpty(accessor.NamespaceName) && accessor.NamespaceName != "<global namespace>")
        {
            sb.AppendLine($"namespace {accessor.NamespaceName};");
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
        string SlotsMapName,
        string EnumName,
        string ExtClassName,
        string NamesClass,
        string? BaseClassName,

        bool IsPartial,
        bool DirectlyImplementsISlots,
        bool IsSealed,

        EquatableArray<string> Hierarchy,
        EquatableArray<string> Properties,
        EquatableArray<string> Slots)
    {
        public Location? Location { get; init; }
    };
}