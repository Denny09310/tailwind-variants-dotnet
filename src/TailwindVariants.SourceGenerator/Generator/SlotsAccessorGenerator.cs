using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace TailwindVariants.SourceGenerator;

[Generator]
public class SlotsAccessorGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor NoPropertiesDescriptor = new(
        id: "TVSG001",
        title: "Slots type contains no public instance properties",
        messageFormat: "The slots type '{0}' contains no public instance properties. No extension methods will be generated.",
        category: "TailwindVariants.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Emit extension methods for SlotMap at post-init so they are available to generated code and consumers
        context.RegisterPostInitializationOutput(ctx =>
            ctx.AddSource("TailwindVariants_SlotMap_Extensions.g.cs",
                          SourceText.From(SourceGenerationHelper.ExtensionMethods, Encoding.UTF8)));

        // Find the SlotMap<> symbol in the compilation
        var slotMapSymbolProvider = context.CompilationProvider
            .Select((comp, _) => comp.GetTypeByMetadataName("TailwindVariants.SlotMap`1"));

        // Collect all type declarations (classes / records / structs) from syntax
        var candidateTypes = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax or StructDeclarationSyntax,
            transform: static (ctx, _) =>
            {
                if (ctx.Node is TypeDeclarationSyntax tds)
                {
                    return ctx.SemanticModel.GetDeclaredSymbol(tds) as INamedTypeSymbol;
                }
                return null;
            })
            .Where(static s => s is not null)!
            .Collect();

        // Combine the slotMap symbol + all candidate types and produce generation callback per compilation
        var combined = slotMapSymbolProvider.Combine(candidateTypes);

        context.RegisterSourceOutput(combined, static (spc, pair) => Execute(pair, spc));
    }

    private static void Execute((INamedTypeSymbol? Left, ImmutableArray<INamedTypeSymbol?> Right) pair, SourceProductionContext spc)
    {
        var slotMapSym = pair.Left;
        var candidates = pair.Right;
        if (slotMapSym is null) return;
        if (candidates.IsDefaultOrEmpty) return;

        // Discover unique slots types used as SlotMap<T>
        var unique = new Dictionary<INamedTypeSymbol, INamedTypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var maybeNamed in candidates)
        {
            if (maybeNamed == null) continue;
            var named = maybeNamed;

            foreach (var member in named.GetMembers())
            {
                INamedTypeSymbol? slotsArg = null;
                if (member is IFieldSymbol fs)
                {
                    if (SymbolHelpers.TryGetSlotMapArgument(fs.Type, slotMapSym, out var a)) slotsArg = a;
                }
                else if (member is IPropertySymbol ps)
                {
                    if (SymbolHelpers.TryGetSlotMapArgument(ps.Type, slotMapSym, out var a)) slotsArg = a;
                }

                if (slotsArg != null && !unique.ContainsKey(slotsArg))
                {
                    unique[slotsArg] = slotsArg;
                }
            }
        }

        foreach (var kv in unique)
        {
            GenerateForSlotsType(kv.Key, spc);
        }
    }

    private static void GenerateForSlotsType(INamedTypeSymbol slotsType, SourceProductionContext spc)
    {
        // Collect public instance properties
        var properties = slotsType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => !p.IsStatic && p.DeclaredAccessibility == Accessibility.Public)
            .ToArray();

        if (properties.Length == 0)
        {
            var diag = Diagnostic.Create(NoPropertiesDescriptor, Location.None, slotsType.ToDisplayString());
            spc.ReportDiagnostic(diag);
            return;
        }

        // Deterministic order: source order if available, fallback to name order
        var ordered = properties.OrderBy(p => p.Locations.FirstOrDefault()?.SourceSpan.Start ?? int.MaxValue)
                                .ThenBy(p => p.Name, StringComparer.Ordinal)
                                .ToArray();

        var slotsTypeName = slotsType.ContainingSymbol?.Name ?? slotsType.Name;

        // Build names
        var enumName = SymbolHelpers.MakeSafeIdentifier(slotsTypeName + "Slots");
        var extClassName = SymbolHelpers.MakeSafeIdentifier(slotsTypeName + "SlotsExtensions");
        var namesClass = SymbolHelpers.MakeSafeIdentifier(slotsTypeName + "SlotsNames");

        var namespaceName = slotsType.ContainingNamespace?.ToDisplayString() ?? string.Empty;

        // Build array of slot property names (string literals)
        var slotNames = ordered.Select(p => p.Name).ToArray();

        // Full type display for the slots generic type argument
        var slotsTypeFull = slotsType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
        var slotMapTypeFull = $"SlotMap<{slotsTypeFull}>";

        // Prepare unique filename
        var fq = slotsType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var assembly = slotsType.ContainingAssembly?.Name ?? "assembly";
        var rawFileName = assembly + "_" + fq + ".SlotsExtensions.g.cs";
        var filename = SymbolHelpers.MakeSafeFileName(rawFileName);

        var sb = new Indenter();
        sb.AppendLine("using TailwindVariants;");
        sb.AppendLine();
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        if (!string.IsNullOrEmpty(namespaceName))
        {
            sb.AppendLine($"namespace {namespaceName};");
            sb.AppendLine();
        }

        // Enum
        sb.AppendLine($"public enum {enumName}");
        sb.AppendLine("{");
        sb.Indent();
        for (int i = 0; i < slotNames.Length; i++)
        {
            var nm = SymbolHelpers.MakeSafeIdentifier(slotNames[i]);
            sb.AppendLine($"{nm} = {i},");
        }
        sb.Dedent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Names helper
        sb.AppendLine($"public static class {namesClass}");
        sb.AppendLine("{");
        sb.Indent();
        sb.AppendLine($"private static readonly string[] _names = new[] {{ {string.Join(", ", slotNames.Select(n => "\"" + n + "\""))} }};");
        sb.AppendLine();
        sb.AppendLine($"public static string NameOf({enumName} key) => _names[(int)key];");
        sb.AppendLine($"public static IReadOnlyList<string> AllNames => _names;");
        sb.Dedent();
        sb.AppendLine("}");
        sb.AppendLine();

        // Extension methods on SlotMap<SlotsType>
        sb.AppendLine($"public static class {extClassName}");
        sb.AppendLine("{");
        sb.Indent();
        sb.AppendLine($"public static string? Get(this {slotMapTypeFull} slots, {enumName} key) => slots.GetByName({namesClass}.NameOf(key));");
        sb.AppendLine($"public static bool TryGet(this {slotMapTypeFull} slots, {enumName} key, out string? value) => slots.TryGetByName({namesClass}.NameOf(key), out value);");
        sb.Dedent();
        sb.AppendLine();

        // Per-property sugar
        sb.Indent();
        foreach (var p in ordered)
        {
            var propName = p.Name;
            var safe = SymbolHelpers.MakeSafeIdentifier(propName);
            sb.AppendLine($"public static string? Get{safe}(this {slotMapTypeFull} slots) => slots.Get({enumName}.{safe});");
            sb.AppendLine($"public static bool TryGet{safe}(this {slotMapTypeFull} slots, out string? value) => slots.TryGet({enumName}.{safe}, out value);");
            sb.AppendLine();
        }
        sb.Dedent();

        sb.AppendLine("}");

        spc.AddSource(filename, SourceText.From(sb.ToString(), Encoding.UTF8));
    }
}