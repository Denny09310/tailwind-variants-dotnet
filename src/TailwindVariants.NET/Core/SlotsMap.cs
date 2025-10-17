using System.Linq.Expressions;

using static TailwindVariants.NET.TvHelpers;

namespace TailwindVariants.NET;

/// <summary>
/// SlotsMap is a simple mapping of slot names to final computed class strings (null allowed).
/// </summary>
public class SlotsMap<TSlots>
	where TSlots : ISlots, new()
{
	private static readonly ClassesAggregator _empty = classes => classes;

	private readonly Dictionary<string, ClassesAggregator> _map = new(StringComparer.Ordinal);

	/// <summary>
	/// Indexer that accepts a slot accessor expression and returns the computed class string or null.
	/// </summary>
	public ClassesAggregator this[Expression<SlotAccessor<TSlots>> key] => this[GetSlot(key)];

	/// <summary>
	/// Indexer that accepts a slot name and returns the computed class string or null.
	/// </summary>
	public ClassesAggregator this[string key] => _map.TryGetValue(key, out var value) ? value : _empty;

	/// <summary>
	/// Create a SlotsMap from a prefilled dictionary.
	/// </summary>
	public static implicit operator SlotsMap<TSlots>(Dictionary<string, ClassesAggregator> map)
	{
		var slots = new SlotsMap<TSlots>();
		foreach (var (key, value) in map)
		{
			slots.Add(key, value);
		}

		return slots;
	}

	/// <summary>
	/// Add a named slot mapping.
	/// </summary>
	internal void Add(string key, ClassesAggregator value) => _map.Add(key, value);
}
