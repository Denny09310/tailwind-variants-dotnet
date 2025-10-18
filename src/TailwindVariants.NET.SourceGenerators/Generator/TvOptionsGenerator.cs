using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace TailwindVariants.NET.SourceGenerators;

[Generator]
public partial class TvOptionsGenerator : IIncrementalGenerator
{
	private const string TvDescriptorTypeName = "TailwindVariants.NET.TvDescriptor`2";

	public void Initialize(IncrementalGeneratorInitializationContext ctx)
	{
		// Keep symbol + node in pipeline (like your SlotsAccessorGenerator)
		var creations = ctx.SyntaxProvider.CreateSyntaxProvider(
			predicate: static (node, _) => IsPotentialTvDescriptor(node),
			transform: static (gctx, _) => GetCreationInfo(gctx))
			.Where(static ci => ci is not null)
			.Select(static (ci, _) => ci!.Value);

		var razorFiles = ctx.AdditionalTextsProvider
			.Where(static at => at.Path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase) ||
								at.Path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
			.Collect();

		var combined = ctx.CompilationProvider.Combine(creations.Collect()).Combine(razorFiles);

		ctx.RegisterSourceOutput(combined, static (spc, data) =>
			Execute(spc, data.Left.Left, data.Left.Right, data.Right));
	}

	private static void Execute(
		SourceProductionContext spc,
		Compilation compilation,
		ImmutableArray<CreationInfo> creations,
		ImmutableArray<AdditionalText> razorFiles)
	{
		if (compilation is null) return;
		if (creations.IsDefaultOrEmpty) return;

		var descriptorSymbol = compilation.GetTypeByMetadataName(TvDescriptorTypeName);
		if (descriptorSymbol is null)
		{
			spc.AddSource("TvOptions_Diagnostic.g.cs",
				SourceText.From($"// TvDescriptor type not found: {TvDescriptorTypeName}", Encoding.UTF8));
			return;
		}

		// Accumulate per-owner (keyed by fully-qualified owner name)
		var perOwner = new Dictionary<string, Accumulator>(StringComparer.Ordinal);

		foreach (var creationInfo in creations)
		{
			try
			{
				// IMPORTANT: ProcessCreation now will register the Owner/Slots even if no variants were present.
				ProcessCreation(creationInfo, descriptorSymbol, perOwner, spc);
			}
			catch (Exception ex)
			{
				var loc = creationInfo.Creation.GetLocation();
				var fname = $"TvOptions_CreationError_{SymbolHelper.MakeSafeFileName(loc.SourceTree?.FilePath ?? "unknown")}_{loc.SourceSpan.Start}.g.cs";
				spc.AddSource(fname, SourceText.From($"// Exception processing TvDescriptor at {loc.GetLineSpan()}:\n// {ex}\n", Encoding.UTF8));
			}
		}

		// Parse Razor files (best-effort) to add property usage
		var razorPropertyUsage = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
		foreach (var razor in razorFiles)
		{
			try
			{
				var text = razor.GetText()?.ToString();
				if (string.IsNullOrWhiteSpace(text)) continue;
				ParseRazorFileForPropertyUsage(text!, razorPropertyUsage);
			}
			catch (Exception ex)
			{
				var outName = $"TvOptions_RazorError_{SymbolHelper.MakeSafeFileName(razor.Path)}.g.cs";
				spc.AddSource(outName, SourceText.From($"// Razor parse error for {razor.Path}:\n// {ex}\n", Encoding.UTF8));
			}
		}

		// Merge razor findings into accumulators
		foreach (var kv in perOwner)
		{
			var acc = kv.Value;
			var ownerSimple = acc.OwnerType.Name;
			if (!razorPropertyUsage.TryGetValue(ownerSimple, out var props)) continue;

			var resolved = props.Select(p =>
			{
				var propSym = FindPropertySymbol(acc.OwnerType, p);
				var tn = propSym?.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "global::System.Object";
				return (p, tn);
			});

			acc.AddProperties(resolved);
		}

		// Emit options class + slot extension files for every owner found
		foreach (var kv in perOwner)
		{
			var acc = kv.Value;
			EmitOptionsAndExtensions(spc, compilation, acc);
		}
	}

	#region Processing / extraction

	private static CreationInfo? GetCreationInfo(GeneratorSyntaxContext ctx)
	{
		if (ctx.Node is not ObjectCreationExpressionSyntax creation) return null;
		// carry the SemanticModel so later passes can use it (same pattern as your other generator)
		return new CreationInfo(creation, ctx.SemanticModel);
	}

	private static bool IsPotentialTvDescriptor(SyntaxNode node)
	{
		if (node is not ObjectCreationExpressionSyntax creation) return false;
		// quick heuristic: has an argument list and a 'variants' named arg
		if (creation.ArgumentList?.Arguments.Count == 0) return false;
		return creation.ArgumentList?.Arguments.Any(a => a.NameColon?.Name.Identifier.Text == "variants") ?? false;
	}

	/// <summary>
	/// Process a candidate creation. Always ensures an Accumulator entry exists for the owner type.
	/// If variants present we extract used properties; if 'extends' is present we capture its type as base for Options.
	/// </summary>
	private static void ProcessCreation(
		CreationInfo info,
		INamedTypeSymbol descriptorSymbol,
		IDictionary<string, Accumulator> perOwner,
		SourceProductionContext spc)
	{
		var creation = info.Creation;
		var model = info.SemanticModel;

		if (model.GetSymbolInfo(creation).Symbol is not IMethodSymbol ctorSymbol) return;
		var containing = ctorSymbol.ContainingType;
		if (containing is null) return;
		if (!SymbolEqualityComparer.Default.Equals(containing.ConstructedFrom, descriptorSymbol)) return;
		if (containing.TypeArguments.Length < 2) return;
		if (containing.TypeArguments[0] is not INamedTypeSymbol ownerType) return;
		if (containing.TypeArguments[1] is not INamedTypeSymbol slotsType) return;

		// Ensure accumulator exists for this owner
		var ownerKey = ownerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		if (!perOwner.TryGetValue(ownerKey, out var acc))
		{
			acc = new Accumulator(ownerType, slotsType, ownerType.ContainingNamespace?.ToDisplayString() ?? string.Empty);
			perOwner[ownerKey] = acc;
		}

		// Extract used properties from variants only if present
		var usedNames = ExtractPropertyNamesFromArguments(creation.ArgumentList, model);
		if (!usedNames.IsDefaultOrEmpty)
		{
			// Resolve declared types for used properties (owner property lookup)
			var usedWithType = new List<(string Name, string TypeName)>();
			foreach (var n in usedNames)
			{
				var prop = FindPropertySymbol(ownerType, n);
				var tn = prop?.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "global::System.Object";
				usedWithType.Add((n, tn));
			}
			acc.AddProperties(usedWithType);
		}

		// Find extends argument (if any) and capture its overall type to be used as base for Options
		var ctor = ctorSymbol;
		var extendsArg = FindArgumentByParameterName(creation.ArgumentList, ctor, "extends");
		if (extendsArg is not null)
		{
			var typeInfo = model.GetTypeInfo(extendsArg.Expression);
			var typeSym = typeInfo.Type as INamedTypeSymbol;
			if (typeSym is not null && !typeSym.IsAnonymousType)
			{
				// check sealed case
				if (typeSym.TypeKind == TypeKind.Class && typeSym.IsSealed)
				{
					spc.ReportDiagnostic(Diagnostic.Create(
						new DiagnosticDescriptor("TV100", "Cannot derive from sealed", "extends {0} is sealed; ignoring extends", "TvOptionsGenerator", DiagnosticSeverity.Warning, true),
						Location.None,
						typeSym.ToDisplayString()));
				}
				else
				{
					var fq = typeSym.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
					acc.SetBaseType(fq);
				}
			}
		}

		// collect slot names
		var slotNames = slotsType.GetMembers().OfType<IPropertySymbol>()
			.Where(p => p.Type.SpecialType == SpecialType.System_String && p.IsVirtualizable())
			.Select(p => p.Name)
			.ToImmutableArray();

		acc.SetSlots(slotNames);
	}

	#endregion

	#region Small helpers (reused)

	private static ArgumentSyntax? FindArgumentByParameterName(ArgumentListSyntax? argList, IMethodSymbol ctorSymbol, string parameterName)
	{
		if (argList is null) return null;

		foreach (var arg in argList.Arguments)
		{
			if (arg.NameColon?.Name.Identifier.Text == parameterName) return arg;
		}

		for (var i = 0; i < argList.Arguments.Count; i++)
		{
			var arg = argList.Arguments[i];
			if (arg.NameColon is not null) continue;
			if (i >= ctorSymbol.Parameters.Length) break;
			if (ctorSymbol.Parameters[i].Name == parameterName) return arg;
		}
		return null;
	}

	private static IPropertySymbol? FindPropertySymbol(INamedTypeSymbol owner, string propertyName)
	{
		for (var cur = owner; cur is not null && cur.SpecialType != SpecialType.System_Object; cur = cur.BaseType)
		{
			var prop = cur.GetMembers(propertyName).OfType<IPropertySymbol>().FirstOrDefault(p => !p.IsStatic && p.DeclaredAccessibility == Accessibility.Public);
			if (prop is not null) return prop;
		}
		return null;
	}

	#endregion
}
