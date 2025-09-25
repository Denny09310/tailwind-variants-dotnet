namespace TailwindVariants;

/// <summary>
/// Non-generic interface for variant definitions that also provide per-slot classes.
/// </summary>
/// <typeparam name="T">The component instance type the variant applies to.</typeparam>
public interface IVariantDefinitionSlots<T>
{
    /// <summary>
    /// Gets the classes for a specific slot name, based on the component instance.
    /// </summary>
    /// <param name="instance">Component instance to evaluate.</param>
    /// <param name="slotName">Slot name.</param>
    /// <returns>Slot-specific CSS classes or <c>null</c>.</returns>
    string? GetSlotFor(T instance, string slotName);
}
