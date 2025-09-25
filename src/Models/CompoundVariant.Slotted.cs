namespace TailwindVariants.Models;

/// <summary>
/// Represents a conditional set of per-slot classes applied when a predicate matches a component instance.
/// </summary>
public record CompoundVariantSlots<T>(Func<T, bool> Predicate, Dictionary<string, string> SlotClasses);
