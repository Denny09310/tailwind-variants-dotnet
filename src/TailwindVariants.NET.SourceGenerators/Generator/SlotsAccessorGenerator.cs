using System.Text;

using Microsoft.CodeAnalysis;
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
			.Where(static symbol => IsMaybeSlotsForGeneration(symbol!))
			.Select(static (symbol, _) => GetTransformedSlots(symbol!))
			.Where(static data => data is not null)
			.Select(static (data, _) => data!.Value);

		context.RegisterSourceOutput(slotsTypes, GenerateForSlotsType);
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
}
