using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace TailwindVariants.NET.SourceGenerators;

[Generator]
public class SlotsAccessorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
		context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
			"SlotAttribute.g.cs",
			SourceText.From(SourceGenerationHelper.Attribute, Encoding.UTF8)));

		var candidateTypes = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (node, _) => IsMaybeCandidateForGeneration(node),
            transform: static (ctx, _) => GetTypeSymbol(ctx))
            .Where(static symbol => symbol is not null);

        var slotsTypes = candidateTypes
            .Where(static symbol => ImplementsISlots(symbol!))
            .Select(static (symbol, _) => BuildGenerationData(symbol!))
            .Where(static data => data is not null)
            .Select(static (data, _) => data!.Value);

        context.RegisterSourceOutput(slotsTypes, GenerateForSlotsType);
    }

	private static SlotsAccessorToGenerate? BuildGenerationData(INamedTypeSymbol symbol)
	{
		// Check inheritance structure
		var inheritanceInfo = AnalyzeInheritance(symbol);

		// Collect properties
		var ownProperties = CollectPropertiesFromType(symbol);
		var allProperties = CollectAllPropertiesInHierarchy(symbol);

		// Build names
		var fullName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
			.Replace("global::", string.Empty);

		// Detect whether the slots type is nested inside another type
		var isNested = symbol.ContainingType is not null;

		// Fully-qualified name of the containing (component) type when nested
		var componentFullName = isNested
			? symbol.ContainingType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", string.Empty)
			: string.Empty;

		var typeName = symbol.ContainingType?.Name
			?? symbol.Name.Replace("Slots", string.Empty);

		// Choose enum/names names conditionally:
		// - if nested => short names that become nested (SlotTypes, SlotNames)
		// - if not nested => keep previous top-level naming (e.g. ItemTitleSlotTypes)
		var enumName = isNested
			? SymbolHelper.MakeSafeIdentifier($"SlotsTypes")
			: SymbolHelper.MakeSafeIdentifier($"{typeName}SlotsTypes");

		var namesClass = isNested
			? SymbolHelper.MakeSafeIdentifier($"SlotsNames")
			: SymbolHelper.MakeSafeIdentifier($"{typeName}SlotsNames");

		return new SlotsAccessorToGenerate(
			Name: symbol.Name,
			FullName: fullName,
			ComponentFullName: componentFullName,
			TypeName: typeName,
			NamespaceName: symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
			Modifiers: BuildModifiersString(symbol),
			BaseClassName: inheritanceInfo.BaseClassName,
			IsDirectImplementation: inheritanceInfo.IsDirectImplementation,
			Hierarchy: BuildTypeHierarchy(symbol),
			Properties: ownProperties,
			AllProperties: allProperties,
			SlotsMapName: $"SlotsMap<{fullName}>",
			EnumName: enumName,
			ExtClassName: SymbolHelper.MakeSafeIdentifier($"{typeName}SlotExtensions"),
			NamesClass: namesClass,
			IsSealed: symbol.IsSealed,
			IsNested: isNested)
		{
			Location = symbol.Locations.FirstOrDefault()
		};
	}

	private static void GenerateForSlotsType(SourceProductionContext spc, SlotsAccessorToGenerate accessor)
	{
		var filename = SymbolHelper.MakeSafeFileName($"{accessor.FullName}.g.cs");

		var sb = new Indenter();
		WritePreamble(sb, accessor);
		WriteNestedOpenings(sb, accessor);
		WriteISlotsClass(sb, accessor);

		if (accessor.IsNested)
		{
			WriteEnum(sb, accessor);
			WriteNamesHelper(sb, accessor);
			WriteNestedClosings(sb, accessor);
		}
		else
		{
			WriteNestedClosings(sb, accessor);
			WriteEnum(sb, accessor);
			WriteNamesHelper(sb, accessor);
		}

		WriteExtensions(sb, accessor);
		WritePragmaClosing(sb);

		spc.AddSource(filename, SourceText.From(sb.ToString(), Encoding.UTF8));
	}

	private static INamedTypeSymbol? GetTypeSymbol(GeneratorSyntaxContext context)
        => context.SemanticModel.GetDeclaredSymbol(context.Node) as INamedTypeSymbol;

    private static bool IsMaybeCandidateForGeneration(SyntaxNode node) =>
        node is TypeDeclarationSyntax tds &&
        tds.Modifiers.Any(SyntaxKind.PartialKeyword) &&
        tds.BaseList is { Types.Count: > 0 } &&
        tds.Members.OfType<PropertyDeclarationSyntax>().Any();

	#region Helpers

	private static InheritanceInfo AnalyzeInheritance(INamedTypeSymbol symbol)
	{
		if (symbol.Interfaces.Any(IsISlotsInterface))
			return new(true, null);

		var baseType = symbol.BaseType;
		if (baseType is { SpecialType: not SpecialType.System_Object } &&
			ImplementsISlots(baseType))
		{
			var baseClass = baseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
									.Replace("global::", string.Empty);
			return new(false, baseClass);
		}

		return new(true, null);
	}

	private static string BuildModifiersString(INamedTypeSymbol type)
	{
		var acc = type.DeclaredAccessibility switch
		{
			Accessibility.Public => "public ",
			Accessibility.Internal => "internal ",
			_ => string.Empty
		};

		var modifier = type.IsSealed ? "sealed " :
					   type.IsAbstract ? "abstract " : string.Empty;

		return $"{acc}{modifier}partial class";
	}

	private static ImmutableArray<string> BuildTypeHierarchy(INamedTypeSymbol type)
	{
		var hierarchy = new Stack<string>();
		var current = type;

		while (current != null)
		{
			hierarchy.Push(current.Name);
			current = current.ContainingType;
		}

		return [.. hierarchy];
	}

	private static ImmutableArray<string> CollectAllPropertiesInHierarchy(INamedTypeSymbol type)
	{
		var properties = new List<string>(8);
		var stack = new Stack<INamedTypeSymbol>();

		for (var current = type;
			 current is { SpecialType: not SpecialType.System_Object };
			 current = current.BaseType)
		{
			if (ImplementsISlots(current))
				stack.Push(current);
		}

		while (stack.Count > 0)
		{
			var currentType = stack.Pop();
			foreach (var prop in currentType.GetMembers().OfType<IPropertySymbol>())
			{
				if (IsPublicStringProperty(prop) &&
					SymbolEqualityComparer.Default.Equals(prop.ContainingType, currentType))
				{
                    properties.Add(prop.Name);
				}
			}
		}

		return [.. properties.OrderBy(name => name, StringComparer.Ordinal)];
	}

	private static ImmutableArray<(string Name, string Slot)> CollectPropertiesFromType(INamedTypeSymbol type)
	{
		return [.. type.GetMembers()
		.OfType<IPropertySymbol>()
		.Where(IsPublicStringProperty)
		.OrderBy(p => (p.Locations.FirstOrDefault()?.SourceSpan.Start ?? int.MaxValue, p.Name))
		.Select(p => (p.Name, GetSlotAttributeName(p) ?? p.Name))];
	}

	private static string? GetSlotAttributeName(IPropertySymbol prop)
	{
		var attr = prop.GetAttributes().FirstOrDefault(IsSlotAttribute);
		if (attr is null) return null;

		foreach (var kv in attr.NamedArguments)
		{
			if (kv.Key == "Name" && kv.Value.Value is string s)
				return s;
		}

		if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string ctor)
			return ctor;

		return null;
	}

	private static bool ImplementsISlots(INamedTypeSymbol type) => type.AllInterfaces.Any(IsISlotsInterface);

	private static bool IsISlotsInterface(INamedTypeSymbol interfaceSymbol)
	{
		return interfaceSymbol.ContainingNamespace?.ToDisplayString() == "TailwindVariants.NET" &&
			   interfaceSymbol.Name == "ISlots";
	}

	private static bool IsPublicStringProperty(IPropertySymbol property) => property switch
	{
		{ IsStatic: true } => false,
		{ DeclaredAccessibility: not Accessibility.Public } => false,
		{ Type.SpecialType: not SpecialType.System_String } => false,
		_ => true
	};

	private static bool IsSlotAttribute(AttributeData ad)
	{
		var cls = ad.AttributeClass;
		if (cls == null) return false;
		return cls.Name is "SlotAttribute" or "Slot"
			|| cls.ToDisplayString() == "TailwindVariants.NET.SlotAttribute";
	}

    #endregion Helpers

    #region Code Writing

    private static void WriteEnum(Indenter sb, SlotsAccessorToGenerate accessor)
    {
        sb.AppendLine();
        sb.AppendLine($"public enum {accessor.EnumName}");
        sb.AppendLine("{");
        sb.Indent();
        // Use AllProperties to include inherited properties
        ImmutableArray<string> properties = accessor.AllProperties;
        for (int i = 0; i < properties.Length; i++)
        {
            var nm = SymbolHelper.MakeSafeIdentifier(properties[i]);
            sb.AppendLine($"{nm} = {i},");
        }
        sb.Dedent();
        sb.AppendLine("}");
        sb.AppendLine();
    }

	private static void WriteExtensions(Indenter sb, SlotsAccessorToGenerate accessor)
	{
		var enumRef = accessor.IsNested
			? $"{accessor.ComponentFullName}.{accessor.EnumName}"
			: accessor.EnumName;

		var namesRef = accessor.IsNested
			? $"{accessor.ComponentFullName}.{accessor.NamesClass}"
			: accessor.NamesClass;

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
        /// Gets the value of the slot identified by the specified <see cref="{{accessor.EnumName}}"/> key.
        /// </summary>
        /// <param name="slots">The <see cref="SlotsMap{T}"/> instance containing slot values.</param>
        /// <param name="key">The enum key representing the slot to retrieve.</param>
        /// <returns>The value of the slot, or <c>null</c> if not set.</returns>
        """);

		sb.AppendLine($"public static string? Get(this {accessor.SlotsMapName} slots, {enumRef} key)");
		sb.Indent();
		sb.AppendLine($" => slots[{namesRef}.NameOf(key)];");
		sb.Dedent();

		foreach (var property in accessor.AllProperties)
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
			sb.AppendLine($" => slots.Get({enumRef}.{safe});");
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

		// generate the required static mapping method that implements ISlots.GetName
		sb.AppendLine("/// <summary>");
		sb.AppendLine("/// Returns the slot name associated with a property (generated mapping).");
		sb.AppendLine("/// </summary>");
		sb.AppendLine("public static string GetName(string slot)");
		sb.AppendLine("{");
		sb.Indent();
		sb.AppendLine("return slot switch");
		sb.AppendLine("{");
		sb.Indent();

		// accessor.Properties is now tuples (PropertyName, SlotName)
		foreach (var (propName, slotName) in accessor.Properties)
		{
			// map nameof(Property) => "slot-name" (literal)
			sb.AppendLine($"nameof({propName}) => {SymbolHelper.QuoteLiteral(slotName)},");
		}

		sb.AppendLine("_ => slot,"); // fallback to the provided value
		sb.Dedent();
		sb.AppendLine("};");
		sb.Dedent();
		sb.AppendLine("}");
		sb.AppendLine();

		// Determine if methods should be virtual or override
		string methodModifier = accessor.IsDirectImplementation
			? (accessor.IsSealed ? "public" : "public virtual")
			: "public override";

		sb.AppendLine($"{methodModifier} IEnumerable<(string Slot, string Value)> EnumerateOverrides()");
		sb.AppendLine("{");
		sb.Indent();

		// If overriding, call base implementation first
		if (!accessor.IsDirectImplementation)
		{
			sb.AppendLine("foreach (var item in base.EnumerateOverrides())");
			sb.Indent();
			sb.AppendLine("yield return item;");
			sb.Dedent();
			sb.AppendLine();
		}

		// Use Properties (own properties only) for EnumerateOverrides
		foreach (var (propName, _) in accessor.Properties)
		{
			sb.AppendLine($"if (!string.IsNullOrWhiteSpace({propName}))");
			sb.Indent();
			sb.AppendLine($"yield return (GetName(nameof({propName})), {propName}!);");
			sb.Dedent();
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

        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// All slot names (read-only).");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public static IReadOnlyList<string> AllNames => Enum.GetNames<{accessor.EnumName}>();");
        sb.AppendLine();
        sb.AppendLine($"/// <summary>Returns the slot name for the given {accessor.EnumName} key.</summary>");
        sb.AppendLine($"public static string NameOf({accessor.EnumName} key) => {accessor.Name}.GetName(key.ToString());");

        sb.Dedent();
        sb.AppendLine("}");

		if (!accessor.IsNested)
		{
			sb.AppendLine();
		}
    }

    private static void WriteNestedClosings(Indenter sb, SlotsAccessorToGenerate accessor)
    {
        ImmutableArray<string> hierarchy = accessor.Hierarchy;
        foreach (var _ in accessor.Hierarchy.Take(hierarchy.Length - 1))
        {
            sb.Dedent();
            sb.AppendLine("}");
        }

		if (accessor.IsNested)
		{
			sb.AppendLine();
		}
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

    private readonly record struct InheritanceInfo(
        bool IsDirectImplementation,
        string? BaseClassName);

	private readonly record struct SlotsAccessorToGenerate(
		string Name,
		string FullName,
		string ComponentFullName,
		string TypeName,
		string NamespaceName,
		string Modifiers,
		string SlotsMapName,
		string EnumName,
		string ExtClassName,
		string NamesClass,
		string? BaseClassName,
		bool IsDirectImplementation,
		bool IsSealed,
		bool IsNested,              
		EquatableArray<string> Hierarchy,
		EquatableArray<(string Name, string Slot)> Properties,
		EquatableArray<string> AllProperties)
	{
		public Location? Location { get; init; }
	};
}
