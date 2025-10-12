namespace TailwindVariants.NET.Tests;

public partial class GhostButtonComponent : ISlotted<GhostButtonSlots>
{
    public string? Class { get; set; }
    public GhostButtonSlots? Classes { get; set; }
    public string? Variant { get; set; }
}