namespace TailwindVariants.NET;

/// <summary>
/// Represents a compiled variant that can be applied to an owner object to aggregate
/// slot-to-class mappings dynamically.
/// </summary>
public interface IApplicableVariant
{
	/// <summary>
	/// Applies the variant logic to the specified <paramref name="owner"/> object,
	/// invoking the provided <paramref name="aggregator"/> for each slot-to-class mapping produced.
	/// </summary>
	/// <param name="owner">The object to which the variant logic is applied.</param>
	/// <param name="aggregator">
	/// A callback that receives each slot name and its corresponding CSS class value.
	/// </param>
	void Apply(object owner, Action<string, string> aggregator);
}
