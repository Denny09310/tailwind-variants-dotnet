using TailwindMerge;

namespace TailwindVariants;

/// <summary>
/// Slotless variant configuration (immutable).
/// </summary>
/// <typeparam name="T">Component type.</typeparam>
public sealed class VariantConfig<T>(
    string baseClasses,
    IVariantDefinition<T>[] variantDefs,
    CompoundVariant<T>[] compound
) : IVariantConfig<T>
{
    private readonly string _base = baseClasses ?? "";
    private readonly CompoundVariant<T>[] _compound = compound ?? [];
    private readonly IVariantDefinition<T>[] _variantDefs = variantDefs ?? [];

    /// <inheritdoc/>
    public string? GetClasses(T instance, TwMerge twMerge, params IEnumerable<string?> classes)
    {
        if (instance is null) throw new ArgumentNullException(nameof(instance));
        ArgumentNullException.ThrowIfNull(twMerge);

        var css = VariantHelpers.BuildCssString(instance, _base, _variantDefs, _compound, classes);
        return twMerge.Merge(css);
    }
}
