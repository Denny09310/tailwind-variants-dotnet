using System.Linq.Expressions;

using static TailwindVariants.NET.TvHelpers;

namespace TailwindVariants.NET;

public interface IOptions
{
	public string? Class { get; set; }
}

public delegate string? ClassResolver(IOptions? options);

/// <summary>
/// SlotsMap is a simple mapping of slot names to final computed class strings (null allowed).
/// </summary>
public class SlotsMap<TSlots>
	where TSlots : ISlots, new()
{
	private readonly Dictionary<string, ClassResolver> _map = new(StringComparer.Ordinal);

	/// <summary>
	/// Indexer that accepts a slot accessor expression and returns the computed class string or null.
	/// </summary>
	public ClassResolver? this[Expression<SlotAccessor<TSlots>> key] => this[GetSlot(key)];

	/// <summary>
	/// Indexer that accepts a slot name and returns the computed class string or null.
	/// </summary>
	public ClassResolver? this[string key] => _map.TryGetValue(key, out var value) ? value : null;

	/// <summary>
	/// Create a SlotsMap from a prefilled dictionary.
	/// </summary>
	public static implicit operator SlotsMap<TSlots>(Dictionary<string, ClassResolver> map)
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
	internal void Add(string key, ClassResolver value) => _map.Add(key, value);
}
