namespace TailwindVariants;

/// <summary>
/// Represents a conditional set of classes applied when a predicate matches a component instance (global classes).
/// </summary>
public record CompoundVariant<T>(Func<T, bool> Predicate, string Classes);
