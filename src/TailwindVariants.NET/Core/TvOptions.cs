namespace TailwindVariants.NET;

/// <summary>
/// The options passed into Tv(...) describing base, variants, slots, and compound variants.
/// </summary>
public class TvOptions<TOwner, TSlots>
    where TSlots : ISlots
{   
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
}
