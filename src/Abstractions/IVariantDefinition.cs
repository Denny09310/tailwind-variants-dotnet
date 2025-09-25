namespace TailwindVariants;

/// <summary>
/// Represents a definition of a variant for a component (e.g., button size or color).
/// </summary>
/// <typeparam name="T">The component instance type the variant applies to.</typeparam>
public interface IVariantDefinition<T>
{
    /// <summary>
    /// Unique key identifying this variant definition, usually derived from the accessor expression.
    /// </summary>
    string Key { get; }

    /// <summary>
    /// Gets the CSS classes for the given instance of the component based on this variant definition.
    /// </summary>
    /// <param name="instance">Component instance to evaluate.</param>
    /// <returns>CSS classes or <c>null</c> if none.</returns>
    string? GetFor(T instance);
}
