using System.Collections.Immutable;
using System.Text;

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace TailwindVariants.NET.SourceGenerators;

public partial class TvOptionsGenerator
{
	private static InheritanceInfo AnalyzeInheritance(INamedTypeSymbol symbol)
	{
		var baseType = symbol.BaseType;
		if (baseType is { SpecialType: not SpecialType.System_Object })
		{
			var baseClass = baseType
				.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
				.Replace("global::", string.Empty);

			return new(false, baseClass);
		}

		return new(true, null);
	}

	private static IPropertySymbol? FindPropertySymbol(INamedTypeSymbol owner, string propertyName)
	{
		for (var cur = owner; cur is not null && cur.SpecialType != SpecialType.System_Object; cur = cur.BaseType)
		{
			var prop = cur.GetMembers(propertyName).OfType<IPropertySymbol>().FirstOrDefault(p =>
				!p.IsStatic && p.DeclaredAccessibility == Accessibility.Public);
			if (prop is not null) return prop;
		}
		return null;
	}

	private static string FindPropertyType(
		INamedTypeSymbol ownerType,
		string propertyName,
		Compilation compilation,
		ImmutableArray<AdditionalText> razorFiles,
		CancellationToken ct)
	{
		// 1) Try semantic lookup on owner + base types
		var propSym = FindPropertySymbol(ownerType, propertyName);
		if (propSym is not null)
		{
			return FormatTypeSymbol(propSym.Type, propSym.NullableAnnotation, ownerType);
		}

		// 2) Try to find a razor file that matches the owner name or owner-name-without-suffixes.

		// if not found by simple name, try best-effort: file containing the owner simple name
		var razorFile = razorFiles.FirstOrDefault(f =>
			Path.GetFileNameWithoutExtension(f.Path).IndexOf(ownerType.Name, StringComparison.OrdinalIgnoreCase) >= 0);

		if (razorFile is null)
		{
			// nothing to fall back to
			return "object";
		}

		// 3) Read razor contents
		var text = razorFile.GetText(ct)?.ToString() ?? string.Empty;
		var path = razorFile.Path;

		// 4) Razor engine -> generated C#
		// Create engine once; it's ok to create per-call but you may want to cache it for speed.
		var fs = RazorProjectFileSystem.Create("/");
		var engine = RazorProjectEngine.Create(RazorConfiguration.Default, fs, builder => { });

		// Some versions accept RazorCodeDocument; create one from source (safer across API versions).
		var projectItem = new InMemoryRazorProjectItem(path, text);
		var codeDoc = engine.Process(projectItem);

		var generatedCSharp = codeDoc.GetCSharpDocument().GeneratedCode;
		if (string.IsNullOrWhiteSpace(generatedCSharp))
			return "object";

		// 5) Parse generated C# using parse options from the consuming compilation to avoid language-version mismatch
		CSharpParseOptions parseOptions = CSharpParseOptions.Default;
		if (compilation is CSharpCompilation csCompilation)
		{
			parseOptions = csCompilation.SyntaxTrees
				.Select(t => t.Options)
				.OfType<CSharpParseOptions>()
				.FirstOrDefault() ?? CSharpParseOptions.Default;
		}

		var tree = CSharpSyntaxTree.ParseText(
			SourceText.From(generatedCSharp, Encoding.UTF8),
			options: parseOptions,
			path: path + ".g.cs");

		// 6) Add tree to compilation to get semantic model (use AddSyntaxTrees to reuse references/options)
		Compilation temp;
		if (compilation is CSharpCompilation cs)
		{
			// Add to existing compilation to better resolve referenced types
			temp = cs.AddSyntaxTrees(tree);
		}
		else
		{
			// fallback: create a small CSharpCompilation with same references
			temp = CSharpCompilation.Create("RazorTempForTvOptions",
				syntaxTrees: [tree],
				references: compilation.References,
				options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
		}

		var model = temp.GetSemanticModel(tree);

		// 7) Find property declaration in generated tree
		var root = tree.GetRoot(ct);
		var propSyntax = root.DescendantNodes()
			.OfType<PropertyDeclarationSyntax>()
			.FirstOrDefault(p => string.Equals(p.Identifier.Text, propertyName, StringComparison.Ordinal));

		if (propSyntax is not null)
		{
			if (model.GetDeclaredSymbol(propSyntax) is IPropertySymbol sym)
			{
				// optionally ensure it's a [Parameter] if you want:
				// var paramAttr = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Components.ParameterAttribute");
				// if (paramAttr != null && !sym.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, paramAttr))) { ... }

				return FormatTypeSymbol(sym.Type, sym.NullableAnnotation, ownerType);
			}
		}

		// Final fallback
		return "object";
	}

	/// <summary>
	/// Format a type symbol into a fully-qualified name; if the symbol displays as a short identifier
	/// (no dots) and it is not a CLR special type (bool/string/...), fallback to prepending the owner's fullname:
	///   OwnerNamespace.OwnerType.ShortName  -> OwnerNamespace.OwnerType.ShortName
	/// </summary>
	private static string FormatTypeSymbol(ITypeSymbol typeSym, NullableAnnotation nullability, INamedTypeSymbol ownerType)
	{
		if (typeSym is null) return "global::System.Object";

		// Handle Nullable<T> (value-type nullable)
		if (typeSym is INamedTypeSymbol nts && nts.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
		{
			var inner = nts.TypeArguments[0];
			var innerName = FormatTypeSymbol(inner, NullableAnnotation.NotAnnotated, ownerType);
			return innerName + "?";
		}

		// Use the full display format
		var raw = typeSym.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

		// strip global:: if you prefer (your code used no global:: in examples)
		raw = raw.Replace("global::", string.Empty);

		// If it's a CLR special type (int, bool, string, etc.) ToDisplayString with UseFullyQualifiedTypeNames will yield "System.Int32" etc.
		// We treat SpecialType != None as NOT to be prepended.
		if (typeSym.SpecialType != SpecialType.None)
		{
			// keep raw as-is, but if you want keyword names (int, string) map them here; otherwise return raw
			// Optionally: map System.String -> string etc.
			if (typeSym.SpecialType == SpecialType.System_String)
				raw = "string";
			else if (typeSym.SpecialType == SpecialType.System_Boolean)
				raw = "bool";
			// ... add more mappings if you want language keywords
			return raw + (typeSym.IsReferenceType && nullability == NullableAnnotation.Annotated ? "?" : string.Empty);
		}

		// If raw already contains a dot, treat it as already qualified.
		if (raw.Contains('.') || raw.Contains('+')) // '+' sometimes used for nested metadata names — be conservative
		{
			return raw + (typeSym.IsReferenceType && nullability == NullableAnnotation.Annotated ? "?" : string.Empty);
		}

		// At this point raw is a single identifier (e.g. "Sizes") and not a CLR special type.
		// Prepend the owner's fully-qualified name (including containing types & namespace).
		var ownerFqn = ownerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", string.Empty);

		// Join them. If owner is "MyNs.Component.Slots" and raw is "Sizes" result: "MyNs.Component.Slots.Sizes"
		// If you prefer "MyNs.Component.Sizes" (i.e. treat Sizes as nested under component, not under Slots),
		// adjust accordingly (owner.Name may end with "Slots").
		var fallback = ownerFqn + "." + raw;

		// Append nullable marker for reference types if annotated
		if (typeSym.IsReferenceType && nullability == NullableAnnotation.Annotated)
			fallback += "?";

		return fallback;
	}

	private static ImmutableArray<string> GetSlotsProperties(INamedTypeSymbol type)
	{
		var properties = new List<string>(8);
		var stack = new Stack<INamedTypeSymbol>();

		for (var current = type;
			 current is { SpecialType: not SpecialType.System_Object };
			 current = current.BaseType)
		{
			if (IsMaybeSlotsForGeneration(current))
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

	private static ImmutableArray<(string, string)> GetVariantsProperties(
		ArgumentListSyntax? argumentList,
		INamedTypeSymbol component,
		Compilation compilation,
		ImmutableArray<AdditionalText> files,
		CancellationToken ct)
	{
		if (argumentList is null) return [];

		var properties = ImmutableArray.CreateBuilder<(string, string)>();

		foreach (var argument in argumentList.Arguments)
		{
			ct.ThrowIfCancellationRequested();

			var argumentName = argument.NameColon?.Name.Identifier.Text;
			if (argumentName is not "variants") continue;

			var initializer = argument.Expression switch
			{
				ImplicitObjectCreationExpressionSyntax impl => impl.Initializer,
				ObjectCreationExpressionSyntax obj => obj.Initializer,
				_ => null
			};

			if (initializer is null) continue;

			foreach (var expr in initializer.Expressions)
			{
				ExpressionSyntax? keyExpr = expr switch
				{
					AssignmentExpressionSyntax { Left: ImplicitElementAccessSyntax imp } => imp.ArgumentList.Arguments.FirstOrDefault()?.Expression,
					ImplicitElementAccessSyntax impOnly => impOnly.ArgumentList.Arguments.FirstOrDefault()?.Expression,
					AssignmentExpressionSyntax { Left: ElementAccessExpressionSyntax e } => e.ArgumentList.Arguments.FirstOrDefault()?.Expression,
					ElementAccessExpressionSyntax eOnly => eOnly.ArgumentList.Arguments.FirstOrDefault()?.Expression,
					_ => null
				};

				if (keyExpr is LambdaExpressionSyntax lambda && lambda.Body is MemberAccessExpressionSyntax ma)
				{
					var name = ma.Name.Identifier.Text;
					var type = FindPropertyType(component, name, compilation, files, ct);

					properties.Add((name, type));
				}
			}
		}

		return [.. properties];
	}

	private static bool HasTwoGenericTypeArguments(TypeSyntax? type) => type switch
	{
		GenericNameSyntax g => g.TypeArgumentList.Arguments.Count == 2,
		QualifiedNameSyntax q when q.Right is GenericNameSyntax right => right.TypeArgumentList.Arguments.Count == 2,
		_ => false
	};

	private static bool IsISlotsInterface(INamedTypeSymbol interfaceSymbol)
	{
		return interfaceSymbol.ContainingNamespace?.ToDisplayString() == "TailwindVariants.NET" &&
			   interfaceSymbol.Name == "ISlots";
	}

	private static bool IsMaybeSlotsForGeneration(INamedTypeSymbol type) => type.AllInterfaces.Any(IsISlotsInterface);

	private static bool IsPublicStringProperty(IPropertySymbol property) => property switch
	{
		{ IsStatic: true } => false,
		{ DeclaredAccessibility: not Accessibility.Public } => false,
		{ Type.SpecialType: not SpecialType.System_String } => false,
		_ => true
	};

	private static bool TryGetArgumentList(GeneratorSyntaxContext context, out ArgumentListSyntax? argumentList)
	{
		argumentList = null;
		var node = context.Node;

		if (node is ObjectCreationExpressionSyntax ocs)
		{
			argumentList = ocs.ArgumentList;
			return argumentList is not null;
		}

		if (node is VariableDeclarationSyntax vd)
		{
			foreach (var variable in vd.Variables)
			{
				var value = variable.Initializer?.Value;
				if (value is ObjectCreationExpressionSyntax ocs2)
				{
					argumentList = ocs2.ArgumentList;
					return true;
				}

				if (value is ImplicitObjectCreationExpressionSyntax iocs)
				{
					argumentList = iocs.ArgumentList;
					return true;
				}
			}
		}

		return false;
	}

	private static bool TryGetGenericTypeSymbol(GeneratorSyntaxContext context, out INamedTypeSymbol? namedTypeSymbol)
	{
		namedTypeSymbol = context.Node switch
		{
			ObjectCreationExpressionSyntax { Type: var type } => context.SemanticModel.GetTypeInfo(type).Type as INamedTypeSymbol,
			VariableDeclarationSyntax { Type: var type } => context.SemanticModel.GetTypeInfo(type).Type as INamedTypeSymbol,
			_ => null
		};

		return namedTypeSymbol is not null;
	}
}
