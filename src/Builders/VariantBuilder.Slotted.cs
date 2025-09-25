using System.Linq.Expressions;
using TailwindVariants.Models;

namespace TailwindVariants;

/// <summary>
/// Builder for components that support typed slots.
/// </summary>
/// <typeparam name="T">Component instance type (must implement <see cref="ISlottableComponent{TSlots}"/>).</typeparam>
/// <typeparam name="TSlots">POCO type representing slots.</typeparam>
public sealed class VariantBuilder<T, TSlots>
    where T : ISlottableComponent<TSlots>
    where TSlots : class, new()
{
    private readonly List<CompoundVariant<T>> _compound = [];
    private readonly List<CompoundVariantSlots<T>> _compoundSlots = [];
    private readonly Dictionary<string, string> _slots = [];
    private readonly List<IVariantDefinition<T>> _variantDefs = [];
    private string _base = "";

    /// <summary>
    /// Set base classes that will always apply.
    /// </summary>
    public VariantBuilder<T, TSlots> Base(params IEnumerable<string> classes)
    {
        _base = string.Join(" ", classes);
        return this;
    }

    /// <summary>
    /// Build the <see cref="VariantConfig{T,TSlots}"/>.
    /// </summary>
    public VariantConfig<T, TSlots> Build() => new(_base, [.. _variantDefs], [.. _compound], [.. _compoundSlots], _slots);

    /// <summary>
    /// Add a global compound rule (applies to component root).
    /// </summary>
    public VariantBuilder<T, TSlots> Compound(Func<T, bool> predicate, string classes)
    {
        _compound.Add(new CompoundVariant<T>(predicate, classes));
        return this;
    }

    /// <summary>
    /// Add a compound slot rule that assigns classes to one or more slot names when predicate matches.
    /// </summary>
    public VariantBuilder<T, TSlots> Compound(Func<T, bool> predicate, Dictionary<string, string> slotClasses)
    {
        _compoundSlots.Add(new CompoundVariantSlots<T>(predicate, slotClasses));
        return this;
    }

    /// <summary>
    /// Register a typed slot and provide its default classes.
    /// The accessor targets a property on the TSlots POCO, e.g. <c>s => s.Avatar</c>.
    /// </summary>
    public VariantBuilder<T, TSlots> Slot(Expression<Func<TSlots, string?>> accessor, string @default = "")
    {
        var key = VariantHelpers.GetVariantKey(accessor);
        _slots[key] = @default;
        return this;
    }

    /// <summary>
    /// Add a slot-aware variant definition mapping a variant value to per-slot classes.
    /// </summary>
    public VariantBuilder<T, TSlots> Variant<TValue>(Expression<Func<T, TValue>> accessor, IDictionary<TValue, Dictionary<string, string>> slotMap)
        where TValue : notnull
    {
        var def = new VariantDefinitionSlots<T, TValue>(accessor, slotMap);
        _variantDefs.Add(def);
        return this;
    }

    /// <summary>
    /// Add a global variant definition mapping a variant value to global classes.
    /// </summary>
    public VariantEntry<T, TValue, TSlots> Variant<TValue>(Expression<Func<T, TValue>> accessor, IDictionary<TValue, string> map)
        where TValue : notnull
    {
        var def = new VariantDefinition<T, TValue>(accessor, map);
        _variantDefs.Add(def);
        return new VariantEntry<T, TValue, TSlots>(this);
    }
}
