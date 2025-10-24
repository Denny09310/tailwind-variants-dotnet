using System.Collections;
using System.Linq.Expressions;

namespace TailwindVariants.NET;

/// <summary>
/// A compound variant: a predicate on owner + slot classes to apply when the predicate matches.
/// </summary>
/// <remarks>
/// Create a compound variant that applies when <paramref name="predicate"/> returns true.
/// </remarks>
public class CompoundVariant<TOwner, TSlots>(Predicate<TOwner> predicate) : IEnumerable<KeyValuePair<Expression<SlotAccessor<TSlots>>, ClassValue>>
	where TSlots : ISlots, new()
	where TOwner : ISlottable<TSlots>
{
	/// <summary>
	/// An optional global class to apply when the predicate matches.
	/// </summary>
	public string? Class { get; set; }

	/// <summary>
	/// The predicate determining when this compound variant applies.
	/// </summary>
	internal Predicate<TOwner> Predicate { get; } = predicate ?? throw new ArgumentNullException(nameof(predicate));

	internal SlotCollection<TSlots> Slots { get; private set; } = [];

	/// <summary>
	/// Indexer to get or set a slot-specific set of classes.
	/// </summary>
	public ClassValue this[Expression<SlotAccessor<TSlots>> key]
	{
		get => Slots[key] ?? throw new InvalidOperationException("Requested slot is not present in compound variant.");
		set => Slots[key] = value;
	}

	/// <summary>
	/// Add per-slot classes for this compound variant.
	/// </summary>
	public void Add(Expression<SlotAccessor<TSlots>> key, ClassValue value)
		=> Slots.Add(key, value);

	/// <inheritdoc/>
	public IEnumerator<KeyValuePair<Expression<SlotAccessor<TSlots>>, ClassValue>> GetEnumerator()
		=> Slots.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
