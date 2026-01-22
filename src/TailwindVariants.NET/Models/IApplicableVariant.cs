namespace TailwindVariants.NET.Models;

/// <summary>
/// Represents a compiled variant that can apply classes.
/// </summary>
/// <typeparam name="TProps">Variant props type.</typeparam>
public interface IApplicableVariant<TProps>
{
	/// <summary>
	/// Applies the variant if conditions are met.
	/// </summary>
	/// <param name="props">Variant props.</param>
	/// <param name="classes">Accumulated class list.</param>
	void Apply(TProps props, List<string> classes);
}
