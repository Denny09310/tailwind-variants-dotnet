using System.Linq.Expressions;
using static TailwindVariants.TvHelpers;

namespace TailwindVariants;

/// <summary>
/// SlotMap is a simple mapping of slot names to final computed class strings (null allowed).
/// </summary>
public class SlotMap<TSlots> where TSlots : ISlots
{
    private readonly Dictionary<string, string?> _map = new(StringComparer.Ordinal);

    /// <summary>
    /// Indexer that accepts a slot accessor expression and returns the computed class string or null.
    /// </summary>
    public string? this[Expression<SlotAccessor<TSlots>> key]
    {
        get => _map.TryGetValue(GetSlot(key), out var value) ? value : null;
        set => _map[GetSlot(key)] = value; // Allow setting to null to clear the slot intentionally
    }

    /// <summary>
    /// Create a SlotMap from a prefilled dictionary.
    /// </summary>
    public static implicit operator SlotMap<TSlots>(Dictionary<string, string?> map)
    {
        var slots = new SlotMap<TSlots>();
        foreach (var (key, value) in map)
        {
            slots.Add(key, value);
        }

        return slots;
    }

    /// <summary>
    /// Add a named slot mapping.
    /// </summary>
    public void Add(string key, string? value) => _map.Add(key, value);
}
