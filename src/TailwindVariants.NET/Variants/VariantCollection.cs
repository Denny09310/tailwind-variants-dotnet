using System.Collections;
using System.Linq.Expressions;

namespace TailwindVariants.NET;

/// <summary>
/// Collection of variant definitions keyed by an accessor expression.
/// </summary>
public class VariantCollection<TOwner, TSlots> : IEnumerable<KeyValuePair<Expression<VariantAccessor<TOwner>>, IVariant<TSlots>>>
    where TSlots : ISlots
{
    private readonly Dictionary<Expression<VariantAccessor<TOwner>>, IVariant<TSlots>> _variants = [];

    public IVariant<TSlots> this[Expression<VariantAccessor<TOwner>> key]
    {
        get => _variants[key];
        set => _variants[key] = value;
    }

    public void Add(Expression<VariantAccessor<TOwner>> key, IVariant<TSlots> value) => _variants.Add(key, value);

    public IEnumerator<KeyValuePair<Expression<VariantAccessor<TOwner>>, IVariant<TSlots>>> GetEnumerator() => _variants.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}