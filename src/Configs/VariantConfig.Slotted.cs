using System.Linq.Expressions;
using TailwindMerge;
using TailwindVariants.Models;

namespace TailwindVariants;

/// <summary>
/// Slot-aware variant configuration (immutable).
/// </summary>
/// <typeparam name="T">Component instance type.</typeparam>
/// <typeparam name="TSlots">Slots POCO type.</typeparam>
public sealed class VariantConfig<T, TSlots>(
    string baseClasses,
    IVariantDefinition<T>[] variantDefs,
    CompoundVariant<T>[] compound,
    CompoundVariantSlots<T>[] compoundSlots,
    Dictionary<string, string> slots)
    where T : ISlottableComponent<TSlots>
    where TSlots : class, new()
{
    private readonly string _base = baseClasses ?? "";
    private readonly CompoundVariant<T>[] _compound = compound ?? [];
    private readonly CompoundVariantSlots<T>[] _compoundSlots = compoundSlots ?? [];
    private readonly Dictionary<string, string> _slots = slots ?? [];
    private readonly IVariantDefinition<T>[] _variantDefs = variantDefs ?? [];

    /// <summary>
    /// Compute merged classes for the component root (global variants + compound).
    /// </summary>
    public string? GetClasses(T instance, TwMerge twMerge, params IEnumerable<string?> classes)
    {
        if (instance is null) throw new ArgumentNullException(nameof(instance));
        ArgumentNullException.ThrowIfNull(twMerge);

        var css = VariantHelpers.BuildCssString(instance, _base, _variantDefs, _compound, classes);
        return twMerge.Merge(css);
    }

    /// <summary>
    /// Compute merged classes for a typed slot. The accessor points to a property on the TSlots POCO (e.g. s => s.Avatar).
    /// The evaluation order is: static slot default → slot-aware variants → compound slot rules → instance override (Classes).
    /// </summary>
    /// <param name="instance">Component instance.</param>
    /// <param name="twMerge">TailwindMerge service.</param>
    /// <param name="accessor">Accessor selecting the slot on the TSlots POCO.</param>
    /// <returns>Merged classes for the slot.</returns>
    public string? GetSlot(T instance, TwMerge twMerge, Expression<Func<TSlots, string?>> accessor)
    {
        if (instance is null) throw new ArgumentNullException(nameof(instance));
        ArgumentNullException.ThrowIfNull(twMerge);
        ArgumentNullException.ThrowIfNull(accessor);

        var key = VariantHelpers.GetVariantKey(accessor);
        var compiled = accessor.Compile();

        // Start with configured default for this slot
        var classes = _slots.TryGetValue(key, out var v) ? v : "";

        // slot-aware variants
        foreach (var def in _variantDefs)
        {
            if (def is IVariantDefinitionSlots<T> slotDef)
            {
                var part = slotDef.GetSlotFor(instance, key);
                if (!string.IsNullOrWhiteSpace(part))
                    classes = string.IsNullOrWhiteSpace(classes) ? part : $"{classes} {part}";
            }
        }

        // compound slot rules
        foreach (var c in _compoundSlots)
        {
            if (c.Predicate(instance) && c.SlotClasses.TryGetValue(key, out var slotClass))
                classes = string.IsNullOrWhiteSpace(classes) ? slotClass : $"{classes} {slotClass}";
        }

        // finally instance override from Classes POCO
        var overrideVal = instance.Classes is not null ? compiled(instance.Classes) : null;
        if (!string.IsNullOrWhiteSpace(overrideVal))
            classes = string.IsNullOrWhiteSpace(classes) ? overrideVal : $"{classes} {overrideVal}";

        return twMerge.Merge(classes);
    }
}
