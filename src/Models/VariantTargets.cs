namespace TailwindVariants;

/// <summary>
/// Represents the root + slot-target mapping for a variant value or compound rule.
/// </summary>
public sealed class VariantTargets(string? root = null, Dictionary<string, string>? slots = null)
{
    /// <summary>
    /// Root/base classes to apply when this variant value (or compound) matches.
    /// </summary>
    public string? Root { get; init; } = root;

    /// <summary>
    /// Slot-specific classes to apply when this variant value (or compound) matches.
    /// Keys are the slot property names (extracted from slot accessors).
    /// </summary>
    public Dictionary<string, string>? Slots { get; init; } = slots;
}