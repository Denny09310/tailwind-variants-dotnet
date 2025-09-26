using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TailwindVariants.SourceGenerator;

public record TvConfigToGenerate(InvocationExpressionSyntax? Invocation, ExpressionSyntax? TargetCreation);
public record InvocationSymbolResult(bool Success, Diagnostic? Diagnostic, INamedTypeSymbol? OwnerSymbol, ITypeSymbol? SlotsSymbol, string? ConfigInitializerText, string? OwnerNamespace, string? TvConfigFq);

[Generator]
public class TvConfigGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<TvConfigToGenerate> invokeCalls = context.SyntaxProvider
          .CreateSyntaxProvider(
              predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
              transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
          .Where(static t => t.Invocation != null && t.TargetCreation != null);

        // Combine with compilation so we can access semantic model / symbols
        var withCompilation = invokeCalls.Combine(context.CompilationProvider);

        // Map each found invocation to a payload with semantic info we need
        var mapped = withCompilation.Select(static (pair, _) =>
        {
            var (invocationPair, compilation) = pair;
            var invocation = invocationPair.Invocation!;
            var targetCreation = invocationPair.TargetCreation!; // object creation expression node

            var semanticModel = compilation.GetSemanticModel(invocation.SyntaxTree);

            // Determine the type of the object creation (should be Tv<TOwner,TSlots>)
            var typeInfo = semanticModel.GetTypeInfo(targetCreation);
            if (typeInfo.Type is not INamedTypeSymbol createdTypeSymbol)
                return new InvocationSymbolResult(false, null, null, null, null, null, null);

            if (!createdTypeSymbol.Name.Equals("Tv", StringComparison.Ordinal) || createdTypeSymbol.TypeArguments.Length < 2)
                return new InvocationSymbolResult(false, null, null, null, null, null, null);

            var ownerTypeArg = createdTypeSymbol.TypeArguments[0] as INamedTypeSymbol;
            var slotsTypeArg = createdTypeSymbol.TypeArguments[1];

            // Find the config argument (constructor arguments)
            ArgumentSyntax? configArgument = null;
            var ctorArgs = targetCreation switch
            {
                ObjectCreationExpressionSyntax oce => oce.ArgumentList?.Arguments,
                ImplicitObjectCreationExpressionSyntax ioc => ioc.ArgumentList?.Arguments,
                _ => null
            };

            if (ctorArgs != null && ctorArgs.Value.Count >= 2)
            {
                configArgument = ctorArgs.Value[1];
            }
            else
            {
                return new InvocationSymbolResult(false, null, ownerTypeArg, slotsTypeArg, null, null, null);
            }

            var cfgExpr = configArgument.Expression;
            InitializerExpressionSyntax? initializer = cfgExpr switch
            {
                ObjectCreationExpressionSyntax oce => oce.Initializer,
                ImplicitObjectCreationExpressionSyntax ioc => ioc.Initializer,
                _ => null
            };
            if (initializer == null)
                return new InvocationSymbolResult(false, null, ownerTypeArg, slotsTypeArg, null, null, null);

            var initializerText = initializer.ToFullString();
            var ownerNamespace = ownerTypeArg?.ContainingNamespace?.ToDisplayString();

            // --- NEW: resolve TvConfig symbol now using the compilation ---
            INamedTypeSymbol? tvConfigSymbol = FindTypeByName(compilation.GlobalNamespace, "TvConfig", 2);

            string? tvConfigFq = null;
            if (tvConfigSymbol != null)
            {
                // fully-qualified format (global::...)
                tvConfigFq = tvConfigSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }

            // We return tvConfigFq inside the OwnerNamespace field (or you can extend the record type).
            // For minimal changes, we can use OwnerNamespace to carry ownerNamespace and
            // repurpose Diagnostic or another field to carry tvConfigFq — but better to extend the record.
            // For clarity, let's return an InvocationSymbolResult and keep tvConfigFq in Diagnostic?.Message (slightly hacky).
            // Better: change InvocationSymbolResult to include TvConfigFq, but for now I'll create a new tuple-like anonymous object.
            return new InvocationSymbolResult(Success: true, Diagnostic: null, OwnerSymbol: ownerTypeArg, SlotsSymbol: slotsTypeArg, ConfigInitializerText: initializerText, OwnerNamespace: ownerNamespace, TvConfigFq: tvConfigFq);
        });

        // Register output: the mapped item is now an anonymous typed value, so adapt the callback
        context.RegisterSourceOutput(mapped, static (spc, item) =>
        {
            // item has fields: Success, OwnerSymbol, SlotsSymbol, ConfigInitializerText, OwnerNamespace, TvConfigFq
            if (!item.Success) return;

            var owner = item.OwnerSymbol!;
            var slots = item.SlotsSymbol!;
            var initializerText = item.ConfigInitializerText!;
            var ownerNamespace = item.OwnerNamespace ?? "GlobalNamespace";
            var tvConfigFq = item.TvConfigFq;

            if (tvConfigFq is null)
            {
                var diag = Diagnostic.Create(new DiagnosticDescriptor(
                    id: "TV001",
                    title: "TvConfig type not found",
                    messageFormat: "Could not find a type named 'TvConfig<TDescriptor,TSlots>' in the compilation. Make sure TvConfig is declared.",
                    category: "TvGenerator",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true),
                    Location.None);
                spc.ReportDiagnostic(diag);
                return;
            }

            var ownerFq = owner.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var slotsFq = slots.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var tvConfigGeneric = $"{tvConfigFq}<{ownerFq}, {slotsFq}>";

            var ownerSimpleName = owner.Name;
            var generatedClassName = $"__TvConfig_Generated_For_{ownerSimpleName}";
            var generatedFieldName = $"__tvConfig_{ownerSimpleName}";

            var source = $$"""
                using System;
                namespace {{ownerNamespace}}
                {
                    public partial class {{ownerSimpleName}}
                    {
                        // Generated by TvConfigGenerator
                        private static readonly {{tvConfigGeneric}} {{generatedFieldName}} = new {{tvConfigGeneric}} {{initializerText}};

                        internal static {{tvConfigGeneric}} GetTvConfig_For_Generator() => {{generatedFieldName}};
                    }
                }
                """;

            spc.AddSource($"{generatedClassName}.g.cs", source);
        });

        // local helper to find type by name and arity (search recursively)
        static INamedTypeSymbol? FindTypeByName(INamespaceSymbol ns, string name, int arity)
        {
            // Search types in this namespace
            foreach (var type in ns.GetTypeMembers())
            {
                if (type.Name == name && type.Arity == arity)
                    return type;
            }

            // Recurse into child namespaces
            foreach (var childNs in ns.GetNamespaceMembers())
            {
                var found = FindTypeByName(childNs, name, arity);
                if (found != null) return found;
            }
            return null;
        }
    }

    private static TvConfigToGenerate GetSemanticTargetForGeneration(GeneratorSyntaxContext ctx)
    {
        var invocation = (InvocationExpressionSyntax)ctx.Node;
        // Inspect the expression being invoked. We expect MemberAccess where .Expression is an ObjectCreationExpression or ImplicitObjectCreationExpression
        if (invocation.Expression is MemberAccessExpressionSyntax ma)
        {
            var targetExpr = ma.Expression;
            // targetExpr should be an object creation (explicit or implicit) expression
            if (targetExpr is ObjectCreationExpressionSyntax || targetExpr is ImplicitObjectCreationExpressionSyntax)
            {
                return new TvConfigToGenerate(Invocation: invocation, TargetCreation: targetExpr);
            }
        }
        return new TvConfigToGenerate(Invocation: null, TargetCreation: null);
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        return node is InvocationExpressionSyntax inv &&
            inv.Expression is MemberAccessExpressionSyntax ma &&
            ma.Name is IdentifierNameSyntax id &&
            id.Identifier.ValueText == "Invoke";
    }
}