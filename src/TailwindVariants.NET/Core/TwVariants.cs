using TailwindVariants.NET.Models;

namespace TailwindVariants.NET.Core;

/// <summary>
/// Strongly-typed Tailwind Variants resolver.
/// Produces a single Tailwind class string.
/// </summary>
/// <typeparam name="TProps">
/// Type describing variant selections.
/// </typeparam>
public sealed class TwVariants<TProps>
{
	private readonly TvDescriptor<TProps> _descriptor;

	private TwVariants(TvDescriptor<TProps> descriptor)
	{
		_descriptor = descriptor;
	}

	/// <summary>
	/// Creates a new <see cref="TwVariants{TProps}"/>.
	/// </summary>
	/// <param name="base">
	/// Base Tailwind classes always applied.
	/// </param>
	/// <param name="variants">
	/// Variant definitions:
	/// variant name → (variant value → class string).
	/// </param>
	/// <param name="compoundVariants">
	/// Compound variants that apply when multiple conditions match.
	/// </param>
	public static TwVariants<TProps> Create(
		string? @base = null,
		Dictionary<string, Dictionary<string, ClassValue>>? variants = null,
		IReadOnlyList<CompiledCompoundVariant<TProps>>? compoundVariants = null
	)
	{
		return new TwVariants<TProps>(
			new TvDescriptor<TProps>(
				@base,
				variants ?? [],
				compoundVariants ?? []
			)
		);
	}

	/// <summary>
	/// Resolves the final Tailwind class string.
	/// </summary>
	/// <param name="props">
	/// Variant selection object.
	/// </param>
	/// <returns>
	/// Space-separated Tailwind class string.
	/// </returns>
	public string Invoke(TProps props)
	{
		var classes = new List<string>();

		if (!string.IsNullOrWhiteSpace(_descriptor.Base))
			classes.Add(_descriptor.Base);

		foreach (var variant in _descriptor.Variants.Values)
			variant.Apply(props, classes);

		foreach (var compound in _descriptor.CompoundVariants)
			compound.Apply(props, classes);

		return string.Join(" ", classes.Where(c => !string.IsNullOrWhiteSpace(c)));
	}
}
