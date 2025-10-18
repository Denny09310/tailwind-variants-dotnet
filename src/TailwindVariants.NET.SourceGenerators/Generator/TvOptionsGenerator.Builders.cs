using System.Collections.Immutable;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TailwindVariants.NET.SourceGenerators;

public partial class TvOptionsGenerator
{
	private static string GuessNestedOrTopLevelTypeReference(
		INamedTypeSymbol owner,
		string nestedName,        // "SlotTypes" or "SlotNames" (the nested name)
		string typeNameFallback,  // e.g. "Button" (used for top-level fallbacks like "ButtonSlotTypes")
		Compilation compilation)
	{
		// 1) Preferred: nested symbol declared directly under the owner (same compilation)
		var nestedSym = owner.GetTypeMembers(nestedName).FirstOrDefault();
		if (nestedSym is not null)
		{
			// use the real fully-qualified display if available
			return nestedSym.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
				.Replace("global::", string.Empty);
		}

		// 2) Try metadata lookup using the metadata name for nested types: Namespace.Owner+Nested
		var ownerNs = owner.ContainingNamespace?.ToDisplayString();
		var ownerMeta = string.IsNullOrEmpty(ownerNs) ? owner.Name : ownerNs + "." + owner.Name;
		var metaNested = ownerMeta + "+" + nestedName; // nested type metadata uses '+'
		var symByMeta = compilation.GetTypeByMetadataName(metaNested);
		if (symByMeta is not null)
		{
			return symByMeta.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
				.Replace("global::", string.Empty);
		}

		// 3) Fallback textual guesses
		// If the owner type itself is nested (Owner is declared inside a component), produce a fully-qualified owner + ".Nested"
		// e.g. My.Company.Components.Button.Slots -> My.Company.Components.Button.SlotTypes
		if (owner.ContainingType is not null)
		{
			var ownerFqn = owner.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", string.Empty);
			return ownerFqn + "." + nestedName;
		}

		// Otherwise owner is top-level: return Component + NestedName (e.g., "Button" + "SlotTypes" => "ButtonSlotTypes")
		// Use the provided typeNameFallback which in your generator is something like: owner.ContainingType?.Name ?? owner.Name.Replace("Slots", "")
		return $"{typeNameFallback}.{nestedName}";
	}


	#region Property extraction & Razor parsing helpers

	/// <summary>
	/// Extract property names from the constructor argument list by scanning only the
	/// first-level element initializers of the 'variants' object initializer.
	/// Accepts shapes like:
	///   variants: new() { [b => b.Variant] = new Variant<...> { ... }, [b => b.Size] = ... }
	/// and ignores nested lambdas inside deeper initializers (e.g. s => s.Base).
	/// </summary>
	private static ImmutableArray<string> ExtractPropertyNamesFromArguments(ArgumentListSyntax? argList, SemanticModel model)
	{
		if (argList is null) return [];

		var result = new HashSet<string>(StringComparer.Ordinal);

		foreach (var arg in argList.Arguments)
		{
			var argName = arg.NameColon?.Name.Identifier.Text;
			if (argName is null || !argName.Equals("variants", StringComparison.Ordinal)) continue;

			// Get the initializer for the variants argument (handles `new() { ... }` and `new Type { ... }`)
			InitializerExpressionSyntax? initializer = arg.Expression switch
			{
				ImplicitObjectCreationExpressionSyntax impl => impl.Initializer,
				ObjectCreationExpressionSyntax obj => obj.Initializer,
				_ => null
			};

			if (initializer is null) continue;

			// Only iterate the top-level expressions in the initializer (these correspond to the [key] = value entries)
			foreach (var expr in initializer.Expressions)
			{
				LambdaExpressionSyntax? keyLambda = null;

				// Pattern: [ lambda ] = value  (AssignmentExpressionSyntax with Left ImplicitElementAccess)
				if (expr is AssignmentExpressionSyntax
					{
						Left: ImplicitElementAccessSyntax
						{
							ArgumentList.Arguments: [{ Expression: var lambdaExpr }]
						}
					})
				{
					keyLambda = lambdaExpr as LambdaExpressionSyntax;
				}
				// Pattern: [ lambda ]  (rare; element access used directly)
				else if (expr is ImplicitElementAccessSyntax
				{
					ArgumentList.Arguments: [{ Expression: var lambdaExpr2 }]
				})
				{
					keyLambda = lambdaExpr2 as LambdaExpressionSyntax;
				}

				if (keyLambda is null) continue;

				// Only accept simple single-parameter lambda forms
				ParameterSyntax? parameter = keyLambda switch
				{
					SimpleLambdaExpressionSyntax s => s.Parameter,
					ParenthesizedLambdaExpressionSyntax p => p.ParameterList.Parameters.FirstOrDefault(),
					_ => null
				};

				if (parameter is null) continue;

				var paramSym = model.GetDeclaredSymbol(parameter);
				if (paramSym is null) continue;

				// Accept only direct member access lambda bodies like: b => b.SomeProp
				if (keyLambda.Body is MemberAccessExpressionSyntax memberAccess)
				{
					var exprSym = model.GetSymbolInfo(memberAccess.Expression).Symbol;
					if (SymbolEqualityComparer.Default.Equals(exprSym, paramSym))
					{
						result.Add(memberAccess.Name.Identifier.Text);
					}
				}
				// If the body is a block or other shape, ignore — we only want the simple key lambdas
			}
		}

		return [.. result.OrderBy(n => n, StringComparer.Ordinal)];
	}

	private static bool IsCommonHtmlAttribute(string name)
	{
		return name switch
		{
			"id" or "class" or "style" or "title" or "type" or "value" or
			"name" or "href" or "src" or "alt" or "width" or "height" or
			"disabled" or "readonly" or "required" or "placeholder" or
			"onclick" or "onchange" or "oninput" or "onsubmit" => true,
			_ when name.StartsWith("data-", StringComparison.Ordinal) => true,
			_ => false
		};
	}

	/// <summary>
	/// Best-effort, regex-based Razor parser to collect component property usage and [Parameter] declarations.
	/// The goal is to catch common patterns like:
	///   &lt;Button Size="Medium" Variant="@Variant.Primary" /&gt;
	/// and in-file parameter declarations:
	///   @attribute [Parameter] public string? Foo { get; set; }
	/// This is intentionally lightweight; for robust parsing use the Razor engine.
	/// </summary>
	private static void ParseRazorFileForPropertyUsage(string razorContent, Dictionary<string, HashSet<string>> propertyUsage)
	{
		if (string.IsNullOrEmpty(razorContent)) return;

		// 1) Match component tag usages and capture component name + attribute name
		// e.g. <Button Size="Large" ...> or <MyNs:Button Size="Large" ...>
		var componentPattern = new Regex(
			@"<\s*([A-Za-z_][\w.:<>+]*)\b([^>]*)>",
			RegexOptions.Compiled | RegexOptions.Singleline);

		// attribute pattern inside a tag: attributeName = " or '
		var attributePattern = new Regex(
			@"\b([A-Za-z_]\w*)\s*=\s*[""']",
			RegexOptions.Compiled);

		foreach (Match m in componentPattern.Matches(razorContent))
		{
			if (m.Groups.Count < 3) continue;
			var compName = m.Groups[1].Value;
			var tagBody = m.Groups[2].Value;

			// find attribute names inside the tag body
			foreach (Match a in attributePattern.Matches(tagBody))
			{
				if (a.Groups.Count < 2) continue;
				var attrName = a.Groups[1].Value;

				// skip common HTML attributes
				if (IsCommonHtmlAttribute(attrName)) continue;

				// Normalize component name: if it contains namespace or `:`, take last segment
				var shortComp = compName.Contains(':') ? compName.Split(':').Last() : compName;
				shortComp = shortComp.Contains('.') ? shortComp.Split('.').Last() : shortComp;

				if (!propertyUsage.TryGetValue(shortComp, out var set))
				{
					set = new HashSet<string>(StringComparer.Ordinal);
					propertyUsage[shortComp] = set;
				}
				set.Add(attrName);
			}
		}

		// 2) Match inline [Parameter] declarations inside the Razor file (very small heuristic)
		// e.g. @attribute [Parameter] public string Foo { get; set; }
		var paramPattern = new Regex(
			@"\[\s*Parameter\s*\]\s*public\s+[A-Za-z0-9_<>,\s]+\s+([A-Za-z_]\w*)\s*\{",
			RegexOptions.Compiled);

		foreach (Match pm in paramPattern.Matches(razorContent))
		{
			if (pm.Groups.Count < 2) continue;
			var paramName = pm.Groups[1].Value;
			// store under a special bucket so caller can decide how to map to owner types
			if (!propertyUsage.TryGetValue("_RazorDeclared", out var set))
			{
				set = new HashSet<string>(StringComparer.Ordinal);
				propertyUsage["_RazorDeclared"] = set;
			}
			set.Add(paramName);
		}
	}

	#endregion Property extraction & Razor parsing helpers
}
