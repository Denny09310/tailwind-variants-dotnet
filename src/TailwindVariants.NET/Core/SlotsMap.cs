using System.Linq.Expressions;
using static TailwindVariants.NET.TvHelpers;

namespace TailwindVariants.NET;

/// <summary>
/// SlotMap is a simple mapping of slot names to final computed class strings (null allowed).
/// </summary>
public class SlotsMap<TSlots> where TSlots : ISlots
{
    private readonly Dictionary<string, string?> _map = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets a read-only dictionary containing key-value pairs mapped by string identifiers.
    /// </summary>
    public IReadOnlyDictionary<string, string?> Map => _map;

    /// <summary>
    /// Indexer that accepts a slot accessor expression and returns the computed class string or null.
    /// </summary>
    public string? this[Expression<SlotAccessor<TSlots>> key]
    {
        get => Map.TryGetValue(GetSlot(key), out var value) ? value : null;
    }

    /// <summary>
    /// Create a SlotMap from a prefilled dictionary.
    /// </summary>
    public static implicit operator SlotsMap<TSlots>(Dictionary<string, string?> map)
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
    internal void Add(string key, string? value) => _map.Add(key, value);
}