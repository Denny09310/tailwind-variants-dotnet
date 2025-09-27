namespace TailwindVariants;

/// <summary>
/// The options passed into Tv(...) describing base, variants, slots, and compound variants.
/// </summary>
public class TvOptions<TOwner, TSlots>
    where TSlots : ISlots
{
    public ClassValue? Base { get; set; }
    public CompoundVariantCollection<TOwner, TSlots>? CompoundVariants { get; set; }
    public SlotCollection<TSlots>? Slots { get; set; }
    public VariantCollection<TOwner, TSlots>? Variants { get; set; }
}
