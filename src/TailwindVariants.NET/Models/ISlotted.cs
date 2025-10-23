namespace TailwindVariants.NET;

/// <summary>
/// Defines a contract for a type that provides slot-based CSS classes.
/// </summary>
/// <typeparam name="TSlots">
/// The type representing the slots, which must implement <see cref="ISlots"/>.
/// </typeparam>
public interface ISlottable<TSlots>
	where TSlots : ISlots
{
	/// <summary>
	/// Gets the base slot CSS classes.
	/// </summary>
	string? Class { get; }

	/// <summary>
	/// Gets the slot-based CSS classes.
	/// </summary>
	TSlots? Classes { get; }
}
