using TailwindMerge;

namespace TailwindVariants;

/// <summary>
/// Contract implemented by the slotless variant configuration (components without slots).
/// </summary>
/// <typeparam name="T">Component type.</typeparam>
public interface IVariantConfig<T>
{
    /// <summary>
    /// Compute merged class string for a component instance using configured variants and optional extra classes.
    /// </summary>
    /// <param name="instance">Component instance.</param>
    /// <param name="twMerge">Tailwind Merge service.</param>
    /// <param name="classes">Additional class collections that will be appended last.</param>
    /// <returns>Merged class string or null.</returns>
    string? GetClasses(T instance, TwMerge twMerge, params IEnumerable<string?> classes);
}
