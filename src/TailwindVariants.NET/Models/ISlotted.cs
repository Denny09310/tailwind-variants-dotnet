namespace TailwindVariants.NET;

/// <summary>
/// Defines a contract for a type that provides slot-based CSS classes.
/// </summary>
/// <typeparam name="TSlots">
/// The type representing the slots, which must implement <see cref="ISlots"/>.
/// </typeparam>
public interface ISlotted<TSlots>
    where TSlots : ISlots
{
    /// <summary>
    /// Gets the slot-based CSS classes.
    /// </summary>
    TSlots? Classes { get; }
}
