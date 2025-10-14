using Microsoft.Extensions.Logging;

namespace TailwindVariants.NET;

/// <summary>
/// Represents a compiled variant that can be applied to an owner object to aggregate
/// slot-to-class mappings dynamically.
/// </summary>
public interface IApplicableVariant
{
	/// <summary>
	/// Applies the variant logic to the specified <paramref name="obj"/> object,
	/// invoking the provided <paramref name="aggregator"/> for each slot-to-class mapping produced.
	/// </summary>
	/// <param name="obj">The object to which the variant logic is applied.</param>
	/// <param name="aggregator">
	/// A callback that receives each slot name and its corresponding CSS class value.
	/// </param>
	/// <param name="factory">A factory to log out eventual warnings/errors.</param>
	void Apply(object obj, Action<string, string> aggregator, ILoggerFactory factory);
}
