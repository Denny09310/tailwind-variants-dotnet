using System.Linq.Expressions;

namespace TailwindVariants;

/// <summary>
/// Slot-aware variant definition: maps a variant value to per-slot classes.
/// </summary>
/// <typeparam name="T">Component type.</typeparam>
/// <typeparam name="TValue">Variant value type.</typeparam>
public sealed class VariantDefinitionSlots<T, TValue> : IVariantDefinition<T>, IVariantDefinitionSlots<T>
    where TValue : notnull
{
    private readonly Func<T, TValue> _accessor;
    private readonly Dictionary<TValue, VariantTargets> _map;

    /// <summary>
    /// Create a slot-aware variant definition.
    /// </summary>
    public VariantDefinitionSlots(Expression<Func<T, TValue>> accessorExpr, IDictionary<TValue, VariantTargets?> map)
    {
        ArgumentNullException.ThrowIfNull(accessorExpr);
        ArgumentNullException.ThrowIfNull(map);

        _accessor = accessorExpr.Compile();
        _map = map.ToDictionary(kvp => kvp.Key, kvp => kvp.Value ?? new VariantTargets());
        Key = VariantHelpers.GetVariantKey(accessorExpr) ?? Guid.NewGuid().ToString();
    }


    /// <inheritdoc/>
    public string Key { get; }

    /// <inheritdoc/>
    public string? GetFor(T instance)
    {
        var value = _accessor(instance);
        if (value is not null && _map.TryGetValue(value, out var vt) && !string.IsNullOrWhiteSpace(vt.Root))
            return vt.Root;
        return null;
    }

    /// <inheritdoc/>
    public string? GetSlotFor(T instance, string slotName)
    {
        var value = _accessor(instance);
        if (value is not null && _map.TryGetValue(value, out var vt) && vt.Slots is not null && vt.Slots.TryGetValue(slotName, out var classes))
            return classes;
        return null;
    }
}
