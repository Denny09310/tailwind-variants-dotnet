using System.Collections;
using System.Linq.Expressions;

namespace TailwindVariants;

/// <summary>
/// A collection mapping Slot accessors to ClassValue objects.
/// </summary>
public class SlotCollection<TSlots>() : IEnumerable<KeyValuePair<Expression<SlotAccessor<TSlots>>, ClassValue>>
    where TSlots : ISlots
{
    private readonly Dictionary<Expression<SlotAccessor<TSlots>>, ClassValue> _slots = [];

    /// <summary>
    /// Create a slot collection whose base slot contains the provided classes.
    /// </summary>
    internal SlotCollection(string classes) : this()
    {
        _slots[b => b.Base] = classes;
    }

    /// <summary>
    /// Indexer (slot accessor) => class value.
    /// </summary>
    public ClassValue this[Expression<SlotAccessor<TSlots>> key]
    {
        get => _slots[key];
        set => _slots[key] = value;
    }

    /// <summary>
    /// Implicit conversion from string to SlotCollection (the string becomes the base slot).
    /// </summary>
    public static implicit operator SlotCollection<TSlots>(string classes) => new(classes);

    /// <summary>
    /// Add a new mapping.
    /// </summary>
    public void Add(Expression<SlotAccessor<TSlots>> key, ClassValue value) => _slots.Add(key, value);

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<Expression<SlotAccessor<TSlots>>, ClassValue>> GetEnumerator() => _slots.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
