using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace TailwindVariants.NET.SourceGenerators;

[Generator]
public partial class SlotsAccessorGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterPostInitializationOutput(ctx =>
		{
			ctx.AddEmbeddedAttributeDefinition();
			ctx.AddSource(
				"SlotAttribute.g.cs",
				SourceText.From(SourceGenerationHelper.Attribute, Encoding.UTF8));
		});

		var candidateTypes = context.SyntaxProvider.CreateSyntaxProvider(
			predicate: static (node, _) => IsMaybeCandidateForGeneration(node),
			transform: static (ctx, _) => GetTransformedCandidate(ctx))
			.Where(static symbol => symbol is not null);

		var slotsTypes = candidateTypes
			.Where(static symbol => IsMaybeSlotForGeneration(symbol!))
			.Select(static (symbol, _) => GetTransformedSlot(symbol!))
			.Where(static data => data is not null)
			.Select(static (data, _) => data!.Value);

		context.RegisterSourceOutput(slotsTypes, GenerateForSlotsType);
	}

	private static void GenerateForSlotsType(SourceProductionContext spc, SlotInfo accessor)
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

	private static INamedTypeSymbol? GetTransformedCandidate(GeneratorSyntaxContext context)
		=> context.SemanticModel.GetDeclaredSymbol(context.Node) as INamedTypeSymbol;

	private static SlotInfo? GetTransformedSlot(INamedTypeSymbol symbol)
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
			? SymbolHelper.MakeSafeIdentifier($"SlotTypes")
			: SymbolHelper.MakeSafeIdentifier($"{typeName}SlotTypes");

		var namesClass = isNested
			? SymbolHelper.MakeSafeIdentifier($"SlotNames")
			: SymbolHelper.MakeSafeIdentifier($"{typeName}SlotNames");

		return new SlotInfo(
			Name: symbol.Name,
			FullName: fullName,
			ComponentFullName: componentFullName,
			TypeName: typeName,
			NamespaceName: symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
			Modifiers: BuildModifiersString(symbol),
			BaseClassName: inheritanceInfo.BaseClassName,
			IsDirectImplementation: inheritanceInfo.IsDirectImplementation,
			IsGetNameImplemented: HasStaticGetNameMethod(symbol),
			Hierarchy: BuildTypeHierarchy(symbol),
			Properties: ownProperties,
			AllProperties: allProperties,
			SlotsMapName: $"SlotsMap<{fullName}>",
			EnumName: enumName,
			ExtClassName: SymbolHelper.MakeSafeIdentifier($"{typeName}SlotExtensions"),
			NamesClass: namesClass,
			IsSealed: symbol.IsSealed,
			IsNested: isNested);
	}

	private static bool IsMaybeCandidateForGeneration(SyntaxNode node) =>
		node is TypeDeclarationSyntax tds &&
		tds.Modifiers.Any(SyntaxKind.PartialKeyword) &&
		tds.BaseList is { Types.Count: > 0 } &&
		tds.Members.OfType<PropertyDeclarationSyntax>().Any();
}
