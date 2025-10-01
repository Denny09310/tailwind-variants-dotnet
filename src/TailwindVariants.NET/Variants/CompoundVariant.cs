using System.Collections;
using System.Linq.Expressions;

namespace TailwindVariants.NET;

/// <summary>
/// A compound variant: a predicate on owner + slot classes to apply when the predicate matches.
/// </summary>
/// <remarks>
/// Create a compound variant that applies when <paramref name="predicate"/> returns true.
/// </remarks>
public class CompoundVariant<TOwner, TSlots>(Predicate<TOwner> predicate) : IEnumerable<KeyValuePair<Expression<VariantAccessor<TSlots>>, SlotCollection<TSlots>>>
    where TSlots : ISlots, new()
    where TOwner : ISlotted<TSlots>
{
    private Dictionary<Expression<VariantAccessor<TSlots>>, SlotCollection<TSlots>>? _values;

    /// <summary>
    /// An optional global class to apply when the predicate matches.
    /// </summary>
    public string? Class { get; set; }

    /// <summary>
    /// The predicate determining when this compound variant applies.
    /// </summary>
    internal Predicate<TOwner> Predicate { get; } = predicate ?? throw new ArgumentNullException(nameof(predicate));

    /// <summary>
    /// Indexer to get or set a slot-specific set of classes.
    /// </summary>
    public SlotCollection<TSlots> this[Expression<VariantAccessor<TSlots>> key]
    {
        get => _values?[key] ?? throw new InvalidOperationException("Requested slot is not present in compound variant.");
        set => (_values ??= [])[key] = value;
    }

    /// <summary>
    /// Add per-slot classes for this compound variant.
    /// </summary>
    public void Add(Expression<VariantAccessor<TSlots>> key, SlotCollection<TSlots> value)
    {
        (_values ??= []).Add(key, value);
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<Expression<VariantAccessor<TSlots>>, SlotCollection<TSlots>>> GetEnumerator()
        => (_values ?? []).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
