using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TailwindVariants.NET.SourceGenerators;

/// <summary>
/// Carries the ObjectCreation node together with the SemanticModel through the incremental pipeline.
/// </summary>
internal readonly record struct CreationInfo(ObjectCreationExpressionSyntax Creation, SemanticModel SemanticModel);

/// <summary>
/// Accumulates discovered information for a single owner component type.
/// </summary>
internal sealed class Accumulator(INamedTypeSymbol ownerType, INamedTypeSymbol slotsType, string ns)
{
	private readonly Dictionary<string, string> _props = new(StringComparer.Ordinal);
	private ImmutableArray<string> _slots = [];

	/// <summary>
	/// Optional fully-qualified base type name when an `extends` was provided and accepted.
	/// </summary>
	public string? BaseTypeName { get; private set; }

	/// <summary>
	/// Owner component type (TOwner).
	/// </summary>
	public INamedTypeSymbol OwnerType { get; } = ownerType ?? throw new ArgumentNullException(nameof(ownerType));

	/// <summary>
	/// Slots type (TSlots).
	/// </summary>
	public INamedTypeSymbol SlotsType { get; } = slotsType ?? throw new ArgumentNullException(nameof(slotsType));

	/// <summary>
	/// Target namespace for generated artifacts (owner's namespace).
	/// </summary>
	public string Namespace { get; } = ns ?? string.Empty;

	/// <summary>
	/// Read-only view of slots collected from the Slots type.
	/// </summary>
	public IEnumerable<string> Slots => _slots;

	/// <summary>
	/// Record the base type name (first one wins; caller may emit diagnostics on conflicts).
	/// The typeSymbol argument is kept for potential future use (not stored here).
	/// </summary>
	public void SetBaseType(string baseTypeName)
	{
		if (string.IsNullOrEmpty(BaseTypeName))
			BaseTypeName = baseTypeName;
	}

	/// <summary>
	/// Add (Name, TypeName) pairs; keep first-seen for each property.
	/// </summary>
	public void AddProperties(IEnumerable<(string Name, string TypeName)> items)
	{
		foreach (var (name, typeName) in items)
		{
			if (!_props.ContainsKey(name))
				_props[name] = typeName ?? "global::System.Object";
		}
	}

	public void SetSlots(ImmutableArray<string> slots) => _slots = slots;

	/// <summary>
	/// Return all discovered properties sorted by name.
	/// </summary>
	public ImmutableArray<(string Name, string TypeName)> GetAllProperties()
	{
		return [.. _props
			.OrderBy(kv => kv.Key, StringComparer.Ordinal)
			.Select(kv => (kv.Key, kv.Value))];
	}
}
