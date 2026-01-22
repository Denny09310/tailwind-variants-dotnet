using TailwindVariants.NET.Models;

namespace TailwindVariants.NET.Core;

/// <summary>
/// Immutable compiled description of a Tailwind Variants configuration.
/// </summary>
/// <typeparam name="TProps">Variant props type.</typeparam>
/// <remarks>
/// Creates a new <see cref="TvDescriptor{TProps}"/>.
/// </remarks>
public sealed class TvDescriptor<TProps>(
	string? baseClasses,
	Dictionary<string, Dictionary<string, ClassValue>> variants,
	IReadOnlyList<CompiledCompoundVariant<TProps>> compoundVariants)
{
	/// <summary>
	/// Base Tailwind classes.
	/// </summary>
	public string? Base { get; } = baseClasses;

	/// <summary>
	/// Compiled variants.
	/// </summary>
	public Dictionary<string, CompiledVariant<TProps>> Variants { get; } = variants.ToDictionary(
			v => v.Key,
			v => new CompiledVariant<TProps>(v.Key, v.Value)
		);

	/// <summary>
	/// Compiled compound variants.
	/// </summary>
	public IReadOnlyList<CompiledCompoundVariant<TProps>> CompoundVariants { get; } = compoundVariants;
}
