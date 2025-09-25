namespace TailwindVariants;

/// <summary>
/// Interface for components that support slot-based class overrides.
/// </summary>
/// <typeparam name="TSlots">Type representing the slot class definitions.</typeparam>
public interface ISlottableComponent<TSlots>
    where TSlots : class, new()
{
    /// <summary>
    /// Gets or sets the slot class overrides for the component.
    /// </summary>
    TSlots? Classes { get; set; }
}
