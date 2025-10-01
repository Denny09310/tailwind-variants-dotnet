namespace TailwindVariants.NET;

/// <summary>
/// Represents a class of slots (the minimal contract).
/// </summary>
public interface ISlots
{
    /// <summary>
    /// The primary/base slot (commonly the root element).
    /// </summary>
    string? Base { get; }

    IEnumerable<(string Slot, string Value)> EnumerateOverrides();
}
