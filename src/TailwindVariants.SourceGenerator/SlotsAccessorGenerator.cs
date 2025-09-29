using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace TailwindVariants.SourceGenerator;

[Generator]
public class SlotsAccessorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Pre-resolve SlotMap<> symbol from the compilation
        var slotMapSymbolProvider = context.CompilationProvider
            .Select((comp, _) => comp.GetTypeByMetadataName("TailwindVariants.SlotMap`1"));

        // Candidate types: every class/record declaration syntax
        var candidateTypes = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax,
            transform: static (ctx, _) =>
            {
                var decl = (TypeDeclarationSyntax)ctx.Node;
                if (ctx.SemanticModel.GetDeclaredSymbol(decl) is INamedTypeSymbol named)
                    return named;
                return null;
            })
            .Where(static s => s != null)!;

        // Combine compilation-level slotMap symbol + candidate types
        var targets = slotMapSymbolProvider.Combine(candidateTypes.Collect())
            .SelectMany(static (pair, _) =>
            {
                var slotMapSymbol = pair.Left;
                var namedSymbols = pair.Right; // ImmutableArray<INamedTypeSymbol?>
                var list = new List<SlotsGenerationTarget>();

                if (slotMapSymbol == null) return list;

                foreach (var maybeNamed in namedSymbols)
                {
                    if (maybeNamed is null) continue;
                    var named = maybeNamed;

                    // Find members that are SlotMap<T>
                    foreach (var member in named.GetMembers())
                    {
                        if (member is IFieldSymbol fs && IsSlotMapOfT(fs.Type, slotMapSymbol, out var tSym))
                        {
                            if (tSym != null)
                                list.Add(new SlotsGenerationTarget(named, member, tSym));
                        }
                        else if (member is IPropertySymbol ps && IsSlotMapOfT(ps.Type, slotMapSymbol, out var tSym2))
                        {
                            if (tSym2 != null)
                                list.Add(new SlotsGenerationTarget(named, member, tSym2));
                        }
                    }
                }

                return list;
            });

        // Collect and register output
        context.RegisterSourceOutput(targets.Collect(), static (spc, arr) => Execute(arr, spc));
    }

    // versione corretta e più robusta
    private static bool IsSlotMapOfT(ITypeSymbol type, INamedTypeSymbol? slotMapSymbol, out INamedTypeSymbol? t)
    {
        t = null;
        if (slotMapSymbol == null) return false;

        if (type is INamedTypeSymbol named)
        {
            // confronta l'OriginalDefinition (stabile) col simbolo SlotMap`1
            if (SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, slotMapSymbol)
                && named.TypeArguments.Length == 1)
            {
                // il type argument può essere qualsiasi ITypeSymbol; proviamo a castare a INamedTypeSymbol se è nominale
                t = named.TypeArguments[0] as INamedTypeSymbol;
                return true;
            }
        }

        return false;
    }


    private static void Execute(ImmutableArray<SlotsGenerationTarget> targets, SourceProductionContext spc)
    {
        if (targets.IsDefaultOrEmpty) return;

        foreach (var target in targets)
        {
            // Validate
            if (target.SlotsType == null) continue;

            var slotProps = target.SlotsType.GetMembers().OfType<IPropertySymbol>()
                .Where(p => !p.IsStatic && p.DeclaredAccessibility == Accessibility.Public).ToArray();

            if (slotProps.Length == 0) continue;

            // Prepare names and full type strings safely
            var container = target.ContainingType;
            var containerFullName = container.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                .Replace("global::", "");
            var slotsTypeFullName = target.SlotsType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                .Replace("global::", "");

            var memberName = target.SlotMapMember.Name;
            var filenameSafe = $"{container.Name}.Slots.g.cs";

            // Decide whether we can generate a partial member on the type (requires type to be partial)
            var canGenerateOnType = container.DeclaringSyntaxReferences
                .Select(r => r.GetSyntax())
                .OfType<TypeDeclarationSyntax>()
                .Any(d => d.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword)));

            string code;
            if (canGenerateOnType)
            {
                // generate nested SlotsAccessors as a partial member
                var sb = new StringBuilder();
                sb.AppendLine("#nullable enable");
                sb.AppendLine();
                sb.AppendLine($"namespace {container.ContainingNamespace.ToDisplayString()};");
                sb.AppendLine();
                sb.AppendLine($"public partial class {container.Name}");
                sb.AppendLine("{");
                sb.AppendLine("    private SlotsAccessors? _accessors;");
                sb.AppendLine();
                sb.AppendLine($"    public SlotsAccessors Accessors => _accessors ??= new SlotsAccessors({memberName});");
                sb.AppendLine();
                sb.AppendLine("    public sealed class SlotsAccessors");
                sb.AppendLine("    {");
                sb.AppendLine($"        private readonly TailwindVariants.SlotMap<{slotsTypeFullName}> _slots;");
                sb.AppendLine();
                sb.AppendLine($"        public SlotsAccessors(TailwindVariants.SlotMap<{slotsTypeFullName}> slots) => _slots = slots;");
                foreach (var p in slotProps)
                {
                    sb.AppendLine();
                    sb.AppendLine($"        public string? {p.Name} => _slots[s => s.{p.Name}];");
                }
                sb.AppendLine("    }");
                sb.AppendLine("}");
                code = sb.ToString();
            }
            else
            {
                // Generate extension methods instead (safe, non-intrusive)
                var sb = new StringBuilder();
                sb.AppendLine("#nullable enable");
                sb.AppendLine();
                sb.AppendLine($"namespace {container.ContainingNamespace.ToDisplayString()};");
                sb.AppendLine();
                sb.AppendLine($"public static class {container.Name}SlotsExtensions");
                sb.AppendLine("{");
                sb.AppendLine($"    public static string? Get{container.Name}Base(this TailwindVariants.SlotMap<{slotsTypeFullName}> slots) => slots[s => s.Base];");
                // A better approach: generate one method per property
                foreach (var p in slotProps)
                {
                    sb.AppendLine();
                    sb.AppendLine($"    public static string? Get{p.Name}(this TailwindVariants.SlotMap<{slotsTypeFullName}> slots) => slots[s => s.{p.Name}];");
                }
                sb.AppendLine("}");
                code = sb.ToString();
            }

            spc.AddSource(filenameSafe, SourceText.From(code, Encoding.UTF8));
        }
    }

    private readonly record struct SlotsGenerationTarget(INamedTypeSymbol ContainingType, ISymbol SlotMapMember, INamedTypeSymbol SlotsType);
}
