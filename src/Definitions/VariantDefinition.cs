using System.Linq.Expressions;

namespace TailwindVariants;

/// <summary>
/// Variant definition mapping a property to global classes.
/// </summary>
/// <typeparam name="T">Component type.</typeparam>
/// <typeparam name="TValue">Variant value type.</typeparam>
public sealed class VariantDefinition<T, TValue> : IVariantDefinition<T>
    where TValue : notnull
{
    private readonly Func<T, TValue> _accessor;
    private readonly Dictionary<TValue, string> _map;

    /// <summary>
    /// Create a new variant definition.
    /// </summary>
    public VariantDefinition(Expression<Func<T, TValue>> accessorExpr, IDictionary<TValue, string> map)
    {
        ArgumentNullException.ThrowIfNull(accessorExpr);
        ArgumentNullException.ThrowIfNull(map);

        _accessor = accessorExpr.Compile();
        _map = new Dictionary<TValue, string>(map);
        Key = VariantHelpers.GetVariantKey(accessorExpr) ?? Guid.NewGuid().ToString();
    }

    /// <inheritdoc/>
    public string Key { get; }

    /// <inheritdoc/>
    public string? GetFor(T instance)
    {
        var value = _accessor(instance);
        if (_map.TryGetValue(value, out var classes)) return classes;
        return null;
    }
}
