using System.Diagnostics.CodeAnalysis;

namespace TailwindVariants.NET;

/// <summary>
/// Generic variant storage interface used by VariantCollection.
/// </summary>
public interface IVariant<TSlots> where TSlots : ISlots
{
    /// <summary>
    /// Try to get the SlotCollection for the given key.
    /// </summary>
    bool TryGetSlots<TKey>(TKey key, [MaybeNullWhen(false)] out SlotCollection<TSlots> slots);
}
