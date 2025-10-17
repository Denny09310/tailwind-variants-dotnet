using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

namespace TailwindVariants.NET.SourceGenerators;

public partial class SlotsAccessorGenerator
{
	private static InheritanceInfo AnalyzeInheritance(INamedTypeSymbol symbol)
	{
		if (symbol.Interfaces.Any(IsISlotsInterface))
			return new(true, null);

		var baseType = symbol.BaseType;
		if (baseType is { SpecialType: not SpecialType.System_Object } &&
			IsMaybeSlotForGeneration(baseType))
		{
			var baseClass = baseType
				.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
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
			if (IsMaybeSlotForGeneration(current))
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

	private static bool IsMaybeSlotForGeneration(INamedTypeSymbol type) => type.AllInterfaces.Any(IsISlotsInterface);

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
}
