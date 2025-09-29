using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace TailwindVariants.NET;

/// <summary>
/// A general-purpose variant keyed by a variant value type.
/// </summary>
public class Variant<TVariant, TSlots> : IVariant<TSlots>,
    IEnumerable<KeyValuePair<TVariant, SlotCollection<TSlots>>>
    where TVariant : notnull
    where TSlots : ISlots
{
    private readonly Dictionary<TVariant, SlotCollection<TSlots>> _variants = [];

    public SlotCollection<TSlots> this[TVariant key]
    {
        get => _variants[key];
        set => _variants[key] = value;
    }

    public void Add(TVariant key, SlotCollection<TSlots> value) => _variants.Add(key, value);

    public IEnumerator<KeyValuePair<TVariant, SlotCollection<TSlots>>> GetEnumerator() => _variants.GetEnumerator();

    public bool TryGetSlots<TKey>(TKey key, [MaybeNullWhen(false)] out SlotCollection<TSlots> slots)
    {
        if (key is TVariant v && _variants.TryGetValue(v, out slots))
        {
            return true;
        }

        slots = null;
        return false;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
