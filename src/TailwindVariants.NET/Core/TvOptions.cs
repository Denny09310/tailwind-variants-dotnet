using static TailwindVariants.NET.TvHelpers;

namespace TailwindVariants.NET;

/// <summary>
/// The options passed into Tv(...) describing base, variants, slots, and compound variants.
/// </summary>
public sealed class TvOptions<TOwner, TSlots>
    where TSlots : ISlots, new()
    where TOwner : ISlotted<TSlots>
{
    public TvOptions(
        ClassValue? @base = null,
        SlotCollection<TSlots>? slots = null,
        VariantCollection<TOwner, TSlots>? variants = null,
        CompoundVariantCollection<TOwner, TSlots>? compoundVariants = null)
    {
        Base = @base;
        Slots = slots;
        Variants = variants;
        CompoundVariants = compoundVariants;

        Precompute();
    }

    /// <summary>
    /// The base CSS classes to apply to the primary slot.
    /// </summary>
    public ClassValue? Base { get; set; }

    /// <summary>
    /// A collection of compound variants, which apply additional classes based on specific predicates.
    /// </summary>
    public CompoundVariantCollection<TOwner, TSlots>? CompoundVariants { get; set; }

    /// <summary>
    /// A collection mapping slot accessors to their corresponding CSS class values.
    /// </summary>
    public SlotCollection<TSlots>? Slots { get; set; }

    /// <summary>
    /// A collection of variant definitions, each keyed by an accessor expression.
    /// </summary>
    public VariantCollection<TOwner, TSlots>? Variants { get; set; }

    internal IReadOnlyDictionary<string, string?> BaseSlots { get; private set; } = default!;

    internal IReadOnlyDictionary<string, CompiledVariant<TOwner, TSlots>> BaseVariants { get; private set; } = default!;

    private void Precompute()
    {
        BaseSlots = PrecomputeBaseAndTopLevelSlots();
        BaseVariants = PrecomputeVariantDefinitions();
    }

    private Dictionary<string, string?> PrecomputeBaseAndTopLevelSlots()
    {
        var map = new Dictionary<string, string?>(StringComparer.Ordinal);

        if (Base is not null)
        {
            map[GetSlot<TSlots>(s => s.Base)] = (string)Base;
        }

        if (Slots is not null)
        {
            foreach (var (key, value) in Slots)
            {
                if (value is not null)
                {
                    map[GetSlot(key)] = (string)value;
                }
            }
        }

        return map;
    }

    private Dictionary<string, CompiledVariant<TOwner, TSlots>> PrecomputeVariantDefinitions()
    {
        var variants = new Dictionary<string, CompiledVariant<TOwner, TSlots>>(StringComparer.Ordinal);

        if (Variants is not null)
        {
            foreach (var (key, value) in Variants)
            {
                var id = key.ToString() ?? Guid.NewGuid().ToString();
                var accessor = key.Compile();
                variants[id] = new CompiledVariant<TOwner, TSlots>(key, value, accessor);
            }
        }

        return variants;
    }
}
