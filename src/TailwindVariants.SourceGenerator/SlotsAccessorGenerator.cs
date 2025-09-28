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
        // Collect all class/record declarations that implement ISlots
        IncrementalValuesProvider<RecordToGenerate?> recordDeclarations =
            context.SyntaxProvider.CreateSyntaxProvider(
                predicate: IsSyntaxTargetForGeneration,
                transform: GetSemanticTargetForGeneration)
            .Where(static s => s is not null)!;

        // Combine all symbols into a collection and generate source
        context.RegisterSourceOutput(recordDeclarations.Collect(),
            static (spc, records) => Execute(records, spc));
    }

    private static void Execute(ImmutableArray<RecordToGenerate?> records, SourceProductionContext spc)
    {
        foreach (var record in records)
        {
            var symbol = record!.Value.Symbol;
            if (symbol == null) continue;

            var slotsMember = symbol.ContainingType?.GetMembers().FirstOrDefault(m =>
            {
                INamedTypeSymbol? named = null;

                if (m is IPropertySymbol prop && prop.Type is INamedTypeSymbol np)
                    named = np;
                else if (m is IFieldSymbol field && field.Type is INamedTypeSymbol nf)
                    named = nf;

                if (named == null) return false;

                // Check that the generic type is SlotMap<T>
                if (named.Name != "SlotMap" || named.TypeArguments.Length != 1)
                    return false;

                // Check that the type argument matches the slots type (symbol)
                return SymbolEqualityComparer.Default.Equals(named.TypeArguments[0], symbol);
            });

            // Skip generating if no SlotMap<TSlots> found
            if (slotsMember == null)
                continue;

            var className = symbol.ContainingSymbol?.Name ?? symbol.Name;
            var namespaceName = symbol.ContainingNamespace?.ToDisplayString() ?? "GlobalNamespace";
            var properties = string.Join("\n\t\t", symbol.GetMembers().OfType<IPropertySymbol>()
                .Select(m => $"public string? {m.Name} => _slots[s => s.{m.Name}];"));

            var code = $$"""
            #nullable enable

            namespace {{namespaceName}};

            public partial class {{className}}
            {
                private SlotsAccessors? _accessors;

                public SlotsAccessors Accessors => _accessors ??= new SlotsAccessors({{slotsMember}});

                public sealed class SlotsAccessors
                {
                    private readonly TailwindVariants.SlotMap<{{symbol}}> _slots;

                    public SlotsAccessors(TailwindVariants.SlotMap<{{symbol}}> slots)
                    {
                        _slots = slots;
                    }

                    {{properties}}
                }
            }
            """;

            spc.AddSource($"{className}.Slots.g.cs", SourceText.From(code, Encoding.UTF8));
        }
    }

    private RecordToGenerate? GetSemanticTargetForGeneration(GeneratorSyntaxContext context, CancellationToken token)
    {
        if (context.Node is ClassDeclarationSyntax or RecordDeclarationSyntax)
        {
            if (context.SemanticModel.GetDeclaredSymbol(context.Node) is INamedTypeSymbol symbol)
            {
                // Check if it implements ISlots
                foreach (var iface in symbol.AllInterfaces)
                {
                    if (iface.ToDisplayString() == "TailwindVariants.ISlots")
                    {
                        return new RecordToGenerate(symbol);
                    }
                }
            }
        }
        return null;
    }

    private bool IsSyntaxTargetForGeneration(SyntaxNode node, CancellationToken token)
    {
        var baseList = node switch
        {
            ClassDeclarationSyntax cds => cds.BaseList,
            RecordDeclarationSyntax rds => rds.BaseList,
            _ => null
        };
        if (baseList == null) return false;
        foreach (var baseType in baseList.Types)
        {
            if (baseType.Type is IdentifierNameSyntax ins && ins.Identifier.Text == "ISlots")
            {
                return true;
            }
            else if (baseType.Type is QualifiedNameSyntax qns && qns.Right.Identifier.Text == "ISlots")
            {
                return true;
            }
        }
        return false;
    }
}

public record struct RecordToGenerate(INamedTypeSymbol? Symbol);