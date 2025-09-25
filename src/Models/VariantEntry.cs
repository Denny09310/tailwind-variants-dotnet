using System.Linq.Expressions;

namespace TailwindVariants;

/// <summary>
/// Fluent entry returned by <see cref="VariantBuilder{T, TSlots}.Variant{TValue}"/> for chaining.
/// </summary>
public sealed class VariantEntry<T, TValue, TSlots>
    where TValue : notnull
    where T : ISlottableComponent<TSlots>
    where TSlots : class, new()
{
    private readonly VariantBuilder<T, TSlots> _parent;

    internal VariantEntry(VariantBuilder<T, TSlots> parent) => _parent = parent;

    /// <summary>
    /// Build the configuration.
    /// </summary>
    public VariantConfig<T, TSlots> Build() => _parent.Build();

    /// <summary>
    /// Add a global compound.
    /// </summary>
    public VariantBuilder<T, TSlots> Compound(Func<T, bool> predicate, string classes) => _parent.Compound(predicate, classes);

    /// <summary>
    /// Register a typed slot.
    /// </summary>
    public VariantBuilder<T, TSlots> Slot(Expression<Func<TSlots, string?>> accessor, string @default = "") => _parent.Slot(accessor, @default);

    /// <summary>
    /// Add another global variant mapping for chaining.
    /// </summary>
    public VariantEntry<T, TNext, TSlots> Variant<TNext>(Expression<Func<T, TNext>> accessor, IDictionary<TNext, string> map)
        where TNext : notnull => _parent.Variant(accessor, map);
}
