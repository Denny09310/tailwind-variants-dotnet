using Tw = TailwindMerge.TwMerge;

namespace TailwindVariants.NET;

/// <summary>
/// A delegate that returns a slot's classes from a typed slots object.
/// </summary>
public delegate string? SlotAccessor<TSlots>(TSlots slots) where TSlots : ISlots;

/// <summary>
/// Return type of Tv: given an owner and a TwMerge instance, returns a SlotMap.
/// </summary>
public delegate SlotMap<TSlots> TvReturnType<TOwner, TSlots>(
    TOwner owner,
    Tw merge
)
    where TSlots : ISlots, new()
    where TOwner : ISlotted<TSlots>;


/// <summary>
/// Variant accessor that given an owner returns an object (selected variant).
/// </summary>
public delegate object? VariantAccessor<TOwner>(TOwner owner);