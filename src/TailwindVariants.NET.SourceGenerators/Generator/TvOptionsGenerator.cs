using System.Collections.Immutable;
using System.Text;

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace TailwindVariants.NET.SourceGenerators;

[Generator]
public class TvOptionsGenerator : IIncrementalGenerator
{
	private const string ParameterAttributeTypeName = "Microsoft.AspNetCore.Components.ParameterAttribute";
	private const string TvDescriptorTypeName = "TailwindVariants.NET.TvDescriptor`2";

	// Public entry
	public void Initialize(IncrementalGeneratorInitializationContext ctx)
	{
		// 1) find object creation expressions in user code
		var creations = ctx.SyntaxProvider.CreateSyntaxProvider(
			predicate: static (node, _) => node is ObjectCreationExpressionSyntax,
			transform: static (ctx, _) => (ObjectCreationExpressionSyntax)ctx.Node);

		// 2) pick up AdditionalFiles (.razor/.cshtml) (collect them into an ImmutableArray once per run)
		var razorFiles = ctx.AdditionalTextsProvider
			.Where(at => at.Path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
					  || at.Path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
			.Collect();

		// 3) combine compilation + creations + razor files for per-run processing
		var combined = ctx.CompilationProvider.Combine(creations.Collect()).Combine(razorFiles);

		// 4) generate metadata per owner type
		ctx.RegisterSourceOutput(combined, (spc, triple) => Execute(spc, triple.Left.Left, triple.Left.Right, triple.Right));
	}

	private static void Execute(SourceProductionContext spc, Compilation? compilation, ImmutableArray<ObjectCreationExpressionSyntax> creations, ImmutableArray<AdditionalText> razorFiles)
	{
		try
		{
			if (compilation is null) return;

			// resolve important symbols once per run
			var paramAttrSymbol = compilation.GetTypeByMetadataName(ParameterAttributeTypeName);
			if (paramAttrSymbol is null) return;

			var descriptorSymbol = compilation.GetTypeByMetadataName(TvDescriptorTypeName);
			if (descriptorSymbol is null) return;

			// Razor engine cached for potential Razor fallback (not used by default)
			var fs = RazorProjectFileSystem.Create("/");
			var razorEngine = RazorProjectEngine.Create(RazorConfiguration.Default, fs, b => { });

			// Collect result groups keyed by owner type full name
			var perOwner = new Dictionary<string, Accumulator>(StringComparer.Ordinal);

			foreach (var creation in creations)
			{
				try
				{
					// For each object creation expression, get a semantic model and symbol info
					var tree = creation.SyntaxTree;
					var model = compilation.GetSemanticModel(tree);

					if (model.GetSymbolInfo(creation).Symbol is not IMethodSymbol ctorSymbol)
					{
						// Not a resolvable constructor invocation
						continue;
					}

					var containingType = ctorSymbol.ContainingType;
					if (containingType is null) continue;

					// Compare constructed-from to the TvDescriptor definition (generic type)
					if (!SymbolEqualityComparer.Default.Equals(containingType.ConstructedFrom, descriptorSymbol)) continue;

					// Get generic arguments: Owner (TOwner) and Slots (TSlots)
					if (containingType.TypeArguments.Length < 2) continue;
					if (containingType.TypeArguments[0] is not INamedTypeSymbol ownerType) continue;
					if (containingType.TypeArguments[1] is not INamedTypeSymbol slotsType) continue;

					// 1) Extract used property names from variants/compoundVariants arguments
					var usedNames = ExtractPropertyNamesFromArguments(creation.ArgumentList, model);

					if (usedNames.Length == 0) continue;

					// 2) Determine 'extends' argument (positional or named)
					var extendsArg = FindArgumentByParameterName(creation.ArgumentList, ctorSymbol, "extends");

					// 3) If 'extends' is present, inspect the object/anonymous initializer and collect types per property name
					var extendsTypes = new Dictionary<string, ITypeSymbol?>(StringComparer.Ordinal);
					if (extendsArg is not null)
					{
						CollectExtendsProvidedTypes(extendsArg.Expression, model, extendsTypes);
					}

					// 4) For each used name, resolve the declared property type on the owner if possible
					var usedWithType = new List<(string Name, string TypeName)>();
					foreach (var name in usedNames)
					{
						// prefer extends-provided type if present
						if (extendsTypes.TryGetValue(name, out var providedType) && providedType is not null)
						{
							usedWithType.Add((name, providedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
							continue;
						}

						// otherwise attempt to find the property symbol on the owner type or base classes
						var propSymbol = FindPropertySymbol(ownerType, name);
						if (propSymbol is not null)
						{
							usedWithType.Add((name, propSymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
							continue;
						}

						// fallback: unknown -> object
						usedWithType.Add((name, "global::System.Object"));
					}

					// 5) Gather slot property names (string properties on the TSlots type)
					var allSlotNames = slotsType.GetMembers()
						.OfType<IPropertySymbol>()
						.Where(p => p.Type.SpecialType == SpecialType.System_String && p.IsVirtualizable())
						.Select(p => p.Name)
						.ToImmutableArray();

					// 6) Accumulate per-owner (group by owner full name)
					var ownerKey = ownerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
					if (!perOwner.TryGetValue(ownerKey, out var acc))
					{
						acc = new Accumulator(ownerType, slotsType, ownerType.ContainingNamespace?.ToDisplayString() ?? string.Empty);
						perOwner[ownerKey] = acc;
					}

					acc.AddProperties(usedWithType);
					acc.SetSlots(allSlotNames);
				}
				catch (OperationCanceledException) { throw; }
				catch (Exception exInner)
				{
					// per-creation exception: surface as generated file so it's easy to debug
					var safe = SafeIdFrom(creation.SyntaxTree.FilePath);
					spc.AddSource($"TvOptions_CreationError_{safe}",
						SourceText.From($"// Exception processing creation at {creation.GetLocation().GetLineSpan()}:\n// {exInner}", Encoding.UTF8));
				}
			}

			// Emit code per owner
			foreach (var kv in perOwner)
			{
				var acc = kv.Value;
				var sb = new StringBuilder();
				sb.AppendLine("// <auto-generated />");
				if (!string.IsNullOrWhiteSpace(acc.Namespace))
				{
					sb.AppendLine($"namespace {acc.Namespace};");
					sb.AppendLine();
				}

				var className = $"{SafeIdFrom(acc.OwnerType.Name)}Options";
				sb.AppendLine($"public static partial class {className}");
				sb.AppendLine("{");

				// emit slots as a string array for convenience
				sb.AppendLine($"    public static readonly string[] Slots = new[] {{ {string.Join(", ", acc.Slots.Select(s => $"\"{s}\""))} }};");

				// emit properties as a nested class with typed properties
				sb.AppendLine($"    public sealed class Options");
				sb.AppendLine("    {");
				sb.AppendLine("        public string? Class { get; set; }");

				foreach (var (name, typeName) in acc.GetAllProperties())
				{
					var safeProp = SafeIdFrom(name);
					sb.AppendLine($"        public {typeName}? {safeProp} {{ get; set; }}");
				}

				sb.AppendLine("    }");
				sb.AppendLine("}");

				var outputName = $"{SafeIdFrom(acc.OwnerType.Name)}Options";
				spc.AddSource(outputName, SourceText.From(sb.ToString(), Encoding.UTF8));
			}
		}
		catch (Exception ex)
		{
			spc.AddSource("TvOptions_GlobalError", SourceText.From("// Exception in TvOptionsGenerator:\n// " + ex.ToString(), Encoding.UTF8));
		}
	}

	#region Helpers

	/// <summary>
	/// If the 'extends' argument is present, analyze the expression and gather provided types for named properties.
	/// Supports anonymous object creation (new { A = expr }) and object-initializer (new T { Prop = expr }).
	/// </summary>
	private static void CollectExtendsProvidedTypes(ExpressionSyntax expression, SemanticModel model, Dictionary<string, ITypeSymbol?> outTypes)
	{
		if (expression is null || model is null) return;

		// anonymous object: new { A = expr, B = expr }
		if (expression is AnonymousObjectCreationExpressionSyntax anon)
		{
			foreach (var member in anon.Initializers)
			{
				// anonymous initializer is an AnonymousObjectMemberDeclaratorSyntax
				// it might have a NameEquals (Name = expr) or just an expression
				var name = member.NameEquals?.Name.Identifier.Text;
				var expr = member.Expression;
				if (name is null) continue;

				var typeInfo = model.GetTypeInfo(expr);
				outTypes[name] = typeInfo.Type;
			}
			return;
		}

		// object creation with initializer: new SomeType { Prop = expr, ... }
		if (expression is ObjectCreationExpressionSyntax objCreation && objCreation.Initializer is InitializerExpressionSyntax init)
		{
			foreach (var initExpr in init.Expressions.OfType<ExpressionSyntax>())
			{
				// look for assignment expressions like Prop = expr
				if (initExpr is AssignmentExpressionSyntax assign &&
					assign.Left is IdentifierNameSyntax leftId)
				{
					var name = leftId.Identifier.Text;
					var rightExpr = assign.Right;
					var typeInfo = model.GetTypeInfo(rightExpr);
					outTypes[name] = typeInfo.Type;
				}
				else if (initExpr is AssignmentExpressionSyntax assign2 &&
					assign2.Left is MemberAccessExpressionSyntax leftMember)
				{
					// e.g. x.Prop = value
					var name = leftMember.Name.Identifier.Text;
					var typeInfo = model.GetTypeInfo(assign2.Right);
					outTypes[name] = typeInfo.Type;
				}
			}
			return;
		}

		// initializer expression directly (rare): { Prop = expr, ... }
		if (expression is InitializerExpressionSyntax initOnly)
		{
			foreach (var initExpr in initOnly.Expressions.OfType<AssignmentExpressionSyntax>())
			{
				if (initExpr.Left is IdentifierNameSyntax leftId)
				{
					var name = leftId.Identifier.Text;
					var typeInfo = model.GetTypeInfo(initExpr.Right);
					outTypes[name] = typeInfo.Type;
				}
			}
			return;
		}

		// Other shapes: could be a reference to an existing anonymous/object variable.
		// Try to get the symbol and its type; if it's an anonymous type or named type we can inspect properties.
		var sym = model.GetSymbolInfo(expression).Symbol;
		if (sym is ILocalSymbol local && local.Type is INamedTypeSymbol localType)
		{
			// If anonymous type, its members are accessible via MemberNames / Properties
			foreach (var m in localType.GetMembers().OfType<IPropertySymbol>())
			{
				outTypes[m.Name] = m.Type;
			}
		}
		else if (sym is IFieldSymbol field && field.Type is INamedTypeSymbol fieldType)
		{
			foreach (var m in fieldType.GetMembers().OfType<IPropertySymbol>())
			{
				outTypes[m.Name] = m.Type;
			}
		}
		else if (sym is IPropertySymbol propSym && propSym.Type is INamedTypeSymbol propType)
		{
			foreach (var m in propType.GetMembers().OfType<IPropertySymbol>())
			{
				outTypes[m.Name] = m.Type;
			}
		}

		// Otherwise we don't know how to extract property-level types from this expression; skip.
	}

	/// <summary> Extract property names referenced by lambdas inside variants / compoundVariants arguments. </summary>
	/// <summary>
	/// Extract property names referenced by *first-level* lambdas inside the
	/// 'variants' / 'compoundVariants' arguments. Nested/sub-lambdas are ignored.
	/// </summary>
	/// <summary>
	/// Extracts property names from *first-level* lambdas used as keys in the
	/// 'variants' or 'compoundVariants' object initializers (e.g., [b => b.Variant]).
	/// Ignores any nested lambdas inside deeper initializers.
	/// </summary>
	private static ImmutableArray<string> ExtractPropertyNamesFromArguments(ArgumentListSyntax? argList, SemanticModel model)
	{
		if (argList is null) return [];

		var propertyNames = new HashSet<string>(StringComparer.Ordinal);

		foreach (var arg in argList.Arguments)
		{
			var argName = arg.NameColon?.Name.Identifier.Text;
			if (argName is not "variants") continue;

			// Find lambdas inside collection initializers
			var lambdas = arg.Expression.DescendantNodes().OfType<LambdaExpressionSyntax>();
			foreach (var lambda in lambdas)
			{
				ParameterSyntax? parameterSyntax = lambda switch
				{
					SimpleLambdaExpressionSyntax s => s.Parameter,
					ParenthesizedLambdaExpressionSyntax p => p.ParameterList.Parameters.FirstOrDefault(),
					_ => null
				};

				if (parameterSyntax is null) continue;

				var parameterSymbol = model.GetDeclaredSymbol(parameterSyntax);
				if (parameterSymbol is null) continue;

				// member access expressions within the lambda body
				var memberAccesses = lambda.DescendantNodes().OfType<MemberAccessExpressionSyntax>();
				foreach (var ma in memberAccesses)
				{
					var exprSymbol = model.GetSymbolInfo(ma.Expression).Symbol;
					if (SymbolEqualityComparer.Default.Equals(exprSymbol, parameterSymbol))
					{
						propertyNames.Add(ma.Name.Identifier.Text);
					}
				}
			}
		}

		return [.. propertyNames.OrderBy(n => n)];
	}

	/// <summary>
	/// Given an argument list and the constructor symbol, find the ArgumentSyntax that corresponds to the named parameter.
	/// Handles both named and positional arguments.
	/// </summary>
	private static ArgumentSyntax? FindArgumentByParameterName(ArgumentListSyntax? argList, IMethodSymbol ctorSymbol, string parameterName)
	{
		if (argList is null) return null;

		// Check named arguments first
		foreach (var arg in argList.Arguments)
		{
			if (arg.NameColon is not null && arg.NameColon.Name.Identifier.Text == parameterName)
				return arg;
		}

		// positional: map position to parameter
		for (var i = 0; i < argList.Arguments.Count; i++)
		{
			var arg = argList.Arguments[i];
			// If argument is omitted or has name-colon it was already handled above
			if (arg.NameColon is not null) continue;
			if (i >= ctorSymbol.Parameters.Length) break;
			var param = ctorSymbol.Parameters[i];
			if (param.Name == parameterName) return arg;
		}

		// As last attempt: try to resolve mapping via parameter symbol info (semantic)
		//for (var i = 0; i < argList.Arguments.Count; i++)
		//{
		//	var arg = argList.Arguments[i];
		//	var param = model.GetSymbolInfo(arg.Expression).Symbol as IParameterSymbol;
		//	// ignore, this mapping is weak — keep for completeness
		//}

		return null;
	}

	/// <summary>
	/// Attempt to find a property symbol on the owner type or its base types.
	/// </summary>
	private static IPropertySymbol? FindPropertySymbol(INamedTypeSymbol owner, string propertyName)
	{
		for (var cur = owner; cur is not null && cur.SpecialType != SpecialType.System_Object; cur = cur.BaseType)
		{
			var prop = cur.GetMembers(propertyName).OfType<IPropertySymbol>().FirstOrDefault(p => !p.IsStatic && p.DeclaredAccessibility == Accessibility.Public);
			if (prop is not null) return prop;
		}
		return null;
	}

	private static string SafeIdFrom(string input)
	{
		if (string.IsNullOrEmpty(input)) return "_";
		var sb = new StringBuilder();
		foreach (var ch in input)
		{
			if (char.IsLetterOrDigit(ch) || ch == '_') sb.Append(ch);
			else sb.Append('_');
		}
		if (char.IsDigit(sb[0])) sb.Insert(0, '_');
		return sb.ToString();
	}

	private sealed class Accumulator(INamedTypeSymbol ownerType, INamedTypeSymbol slotsType, string ns)
	{
		private readonly Dictionary<string, string> _props = new(StringComparer.Ordinal);
		private ImmutableArray<string> _slots = [];

		public string Namespace { get; } = ns;
		public INamedTypeSymbol OwnerType { get; } = ownerType;
		public IEnumerable<string> Slots => _slots;
		public INamedTypeSymbol SlotsType { get; } = slotsType;
		public void AddProperties(IEnumerable<(string Name, string TypeName)> items)
		{
			foreach (var (n, t) in items)
			{
				if (!_props.ContainsKey(n))
					_props[n] = t;
			}
		}

		public ImmutableArray<(string Name, string TypeName)> GetAllProperties()
			=> [.. _props.Select(kv => (kv.Key, kv.Value))];

		public void SetSlots(ImmutableArray<string> slots) => _slots = slots;
	}
	#endregion
}
