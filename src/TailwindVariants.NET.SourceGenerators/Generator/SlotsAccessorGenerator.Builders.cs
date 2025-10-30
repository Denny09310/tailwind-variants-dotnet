using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TailwindVariants.NET.SourceGenerators;

public partial class SlotsAccessorGenerator
{
	private static InheritanceInfo AnalyzeInheritance(INamedTypeSymbol symbol)
	{
		if (symbol.Interfaces.Any(IsISlotsInterface))
			return new(true, null);

		var baseType = symbol.BaseType;
		if (baseType is { SpecialType: not SpecialType.System_Object } &&
			IsMaybeSlotsForGeneration(baseType))
		{
			var baseClass = baseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
									.Replace("global::", string.Empty);
			return new(false, baseClass);
		}

		return new(true, null);
	}

	private static ImmutableArray<(string Name, string Modifiers)> BuildTypeHierarchy(INamedTypeSymbol type)
	{
		var stack = new Stack<(string, string)>();
		var current = type;

		while (current != null)
		{
			var name = current.Name;
			var typeParameters = GetTypeParameters(current);

			var modifiers = BuildTypeModifiersString(current);
			stack.Push(($"{name}{typeParameters}", modifiers));
			current = current.ContainingType;
		}

		// return from outermost -> innermost
		return [.. stack];
	}

	private static string BuildTypeModifiersString(INamedTypeSymbol type)
	{
		// Accessibility
		string acc = type.DeclaredAccessibility switch
		{
			Accessibility.Public => "public",
			Accessibility.Internal => "internal",
			Accessibility.Private => "private",
			Accessibility.Protected => "protected",
			Accessibility.ProtectedAndInternal => "private protected",
			Accessibility.ProtectedOrInternal => "protected internal",
			_ => string.Empty
		};

		// class/struct/interface
		var kind = type.TypeKind == TypeKind.Struct ? "struct" :
				   type.TypeKind == TypeKind.Interface ? "interface" :
				   "class";

		// other modifiers: prefer static, otherwise sealed/abstract
		var extra = type.IsStatic ? "static " :
					type.IsSealed ? "sealed " :
					type.IsAbstract ? "abstract " : string.Empty;

		// Always generate 'partial' so generated nested partial declarations are allowed
		var partial = "partial";

		// Combine (only add spaces where needed)
		var parts = new List<string>();
		if (!string.IsNullOrEmpty(acc)) parts.Add(acc);
		if (!string.IsNullOrEmpty(extra)) parts.Add(extra.Trim());
		parts.Add(partial);
		parts.Add(kind);

		return string.Join(" ", parts);
	}

	private static ImmutableArray<string> CollectAllPropertiesInHierarchy(INamedTypeSymbol type)
	{
		var properties = new List<string>(8);
		var stack = new Stack<INamedTypeSymbol>();

		for (var current = type;
			 current is { SpecialType: not SpecialType.System_Object };
			 current = current.BaseType)
		{
			if (IsMaybeSlotsForGeneration(current))
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

	private static INamedTypeSymbol? GetTransformedCandidate(GeneratorSyntaxContext context)
		=> context.SemanticModel.GetDeclaredSymbol(context.Node) as INamedTypeSymbol;

	private static SlotsAccessorToGenerate? GetTransformedSlots(INamedTypeSymbol symbol)
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

		var componentSymbol = symbol.ContainingType;

		string componentTypeParameters = GetTypeParameters(componentSymbol);
		string componentFullName = componentFullName = componentSymbol?
			.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
			.Replace("global::", string.Empty) ?? string.Empty;

		var typeName = symbol.ContainingType?.Name
			?? symbol.Name.Replace("Slots", string.Empty);

		var slotsMapName = $"SlotsMap<{fullName}>";

		// Choose enum/names names conditionally:
		// - if nested => short names that become nested (SlotTypes, SlotNames)
		// - if not nested => keep previous top-level naming (e.g. ItemTitleSlotTypes)
		var enumName = isNested
			? SymbolHelper.MakeSafeIdentifier($"SlotTypes")
			: SymbolHelper.MakeSafeIdentifier($"{typeName}SlotTypes");

		var namesClass = isNested
			? SymbolHelper.MakeSafeIdentifier($"SlotNames")
			: SymbolHelper.MakeSafeIdentifier($"{typeName}SlotNames");

		return new SlotsAccessorToGenerate(
			Name: symbol.Name,
			FullName: fullName,
			ComponentFullName: componentFullName,
			ComponentTypeParameters: componentTypeParameters,
			TypeName: typeName,
			NamespaceName: symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
			Modifiers: BuildTypeModifiersString(symbol),
			BaseClassName: inheritanceInfo.BaseClassName,
			IsDirectImplementation: inheritanceInfo.IsDirectImplementation,
			IsGetNameImplemented: HasStaticGetNameMethod(symbol),
			Hierarchy: BuildTypeHierarchy(symbol),
			Properties: ownProperties,
			AllProperties: allProperties,
			SlotsMapName: slotsMapName,
			EnumName: enumName,
			ExtClassName: SymbolHelper.MakeSafeIdentifier($"{typeName}SlotExtensions"),
			NamesClass: namesClass,
			IsSealed: symbol.IsSealed,
			IsNested: isNested);
	}

	private static string GetTypeParameters(INamedTypeSymbol? componentSymbol)
	{
		if (componentSymbol is null || componentSymbol.TypeParameters.Length <= 0)
		{
			return string.Empty;
		}

		// definition side: <T, U>
		return "<" + string.Join(
			", ",
			componentSymbol.TypeParameters.Select(tp => tp.Name)
		) + ">";
	}

	private static bool HasStaticGetNameMethod(INamedTypeSymbol type)
	{
		foreach (var m in type.GetMembers("GetName").OfType<IMethodSymbol>())
		{
			// public static string GetName(string)
			if (m.IsStatic &&
				m.DeclaredAccessibility == Accessibility.Public &&
				m.Parameters.Length == 1 &&
				m.Parameters[0].Type.SpecialType == SpecialType.System_String &&
				m.ReturnType.SpecialType == SpecialType.System_String)
			{
				return true;
			}
		}

		return false;
	}

	private static bool IsISlotsInterface(INamedTypeSymbol interfaceSymbol)
	{
		return interfaceSymbol.ContainingNamespace?.ToDisplayString() == "TailwindVariants.NET" &&
			   interfaceSymbol.Name == "ISlots";
	}

	private static bool IsMaybeCandidateForGeneration(SyntaxNode node) =>
		node is TypeDeclarationSyntax tds &&
		tds.Modifiers.Any(SyntaxKind.PartialKeyword) &&
		tds.BaseList is { Types.Count: > 0 } &&
		tds.Members.OfType<PropertyDeclarationSyntax>().Any();

	private static bool IsMaybeSlotsForGeneration(INamedTypeSymbol type) => type.AllInterfaces.Any(IsISlotsInterface);

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
}
