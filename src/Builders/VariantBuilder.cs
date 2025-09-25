using System.Linq.Expressions;

namespace TailwindVariants;

/// <summary>
/// Builder for slotless components (no slots).
/// </summary>
/// <typeparam name="T">Component instance type.</typeparam>
public sealed class VariantBuilder<T>
{
    private readonly List<CompoundVariant<T>> _compound = [];
    private readonly List<IVariantDefinition<T>> _variantDefs = [];
    private string _base = "";

    /// <summary>
    /// Set base classes that will always apply.
    /// </summary>
    public VariantBuilder<T> Base(string classes)
    {
        _base = classes ?? "";
        return this;
    }

    /// <summary>
    /// Build the slotless <see cref="VariantConfig{T}"/>.
    /// </summary>
    public VariantConfig<T> Build() => new(_base, [.. _variantDefs], [.. _compound]);

    /// <summary>
    /// Add a global compound rule.
    /// </summary>
    public VariantBuilder<T> Compound(Func<T, bool> predicate, string classes)
    {
        _compound.Add(new CompoundVariant<T>(predicate, classes));
        return this;
    }

    /// <summary>
    /// Add a typed variant mapping for global classes.
    /// </summary>
    public VariantBuilder<T> Variant<TValue>(Expression<Func<T, TValue>> accessor, IDictionary<TValue, string> map)
        where TValue : notnull
    {
        var def = new VariantDefinition<T, TValue>(accessor, map);
        _variantDefs.Add(def);
        return this;
    }
}
