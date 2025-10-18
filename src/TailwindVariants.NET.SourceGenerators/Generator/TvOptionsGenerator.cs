using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace TailwindVariants.NET.SourceGenerators;

[Generator]
public partial class TvOptionsGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext ctx)
	{
		var candidateTypes = ctx.SyntaxProvider.CreateSyntaxProvider(
			predicate: static (node, _) => IsMaybeDescriptor(node),
			transform: static (gctx, _) => GetTransformedDescriptors(gctx))
			.WithComparer(DescriptorComparer.Instance)
			.Where(static ci => ci is not null)
			.Select(static (ci, _) => ci!.Value)
			.Collect();

		var razorFiles = ctx.AdditionalTextsProvider
			.Where(static at =>
				at.Path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase) ||
				at.Path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
			.Collect();

		var descriptorTypes = ctx.CompilationProvider
			.Combine(candidateTypes)
			.Combine(razorFiles)
			.Select(static (di, ct) => GetTransformedDescriptorInfo(di.Left.Left, di.Left.Right, di.Right, ct));

		ctx.RegisterSourceOutput(descriptorTypes, GenerateForComponentType);
	}

	private static ImmutableArray<OptionsInfo>? GetTransformedDescriptorInfo(
		Compilation compilation,
		ImmutableArray<DescriptorInfo> descriptors,
		ImmutableArray<AdditionalText> files,
		CancellationToken ct)
	{
		if (descriptors.IsEmpty) return null;

		var results = ImmutableArray.CreateBuilder<OptionsInfo>();

		try
		{
			foreach (var (arguments, component, slots) in descriptors)
			{
				ct.ThrowIfCancellationRequested();

				var inheritance = AnalyzeInheritance(component);

				var fullName =
					component.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", string.Empty);

				var componentName
					= component.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

				var slotsName =
					slots.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", string.Empty);

				var isNested =
					slots.ContainingType is not null;

				var extName = $"{componentName}SlotExtensions";
				var optionsName = $"{componentName}Options";
				var enumFullName = $"{fullName}{(isNested ? "." : "")}SlotTypes";
				var namesName = $"{fullName}{(isNested ? "." : "")}SlotNames";

				var slotsProperties = GetSlotsProperties(slots);
				var variantsProperties = GetVariantsProperties(arguments, component, compilation, files, ct);

				results.Add(new OptionsInfo(
					FullName: $"{fullName}Options",
					SlotsClassName: slotsName,
					EnumClassName: enumFullName,
					OptionsClassName: optionsName,
					NamesClassName: namesName,
					ExtClassName: extName,
					Inheritance: inheritance,
					NamespaceName: component.ContainingNamespace?.ToDisplayString() ?? string.Empty,
					SlotsTypeName: $"SlotsMap<{slotsName}, {optionsName}>",
					SlotsProperties: slotsProperties,
					VariantsProperties: variantsProperties));
			}
		}
		catch (OperationCanceledException) { }

		return [.. results];
	}

	private static DescriptorInfo? GetTransformedDescriptors(GeneratorSyntaxContext context)
	{
		if (!TryGetGenericTypeSymbol(context, out var namedTypeSymbol))
			return null;

		if (!TryGetArgumentList(context, out var argumentList))
			return null;

		if (namedTypeSymbol is not { Name: "TvDescriptor", TypeArguments.Length: 2 })
			return null;

		if (namedTypeSymbol.TypeArguments[0] is not INamedTypeSymbol componentType)
			return null;

		if (namedTypeSymbol.TypeArguments[1] is not INamedTypeSymbol slotsType)
			return null;

		return new(argumentList, componentType, slotsType);
	}

	private static bool IsMaybeDescriptor(SyntaxNode node) =>
		node is ObjectCreationExpressionSyntax { Type: GenericNameSyntax { TypeArgumentList.Arguments.Count: 2 } }
			or VariableDeclarationSyntax { Type: GenericNameSyntax { TypeArgumentList.Arguments.Count: 2 } };

	private void GenerateForComponentType(SourceProductionContext spc, ImmutableArray<OptionsInfo>? options)
	{
		if (options is null) return;

		foreach (var option in options)
		{
			var filename = SymbolHelper.MakeSafeFileName($"{option.FullName}.g.cs");

			var sb = new Indenter();

			WritePreamble(sb, option);
			WriteOptions(sb, option);
			WriteExtensions(sb, option);
			WritePragmaClosing(sb);

			spc.AddSource(filename, SourceText.From(sb.ToString(), Encoding.UTF8));
		}
	}
}
