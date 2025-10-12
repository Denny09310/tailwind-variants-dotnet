namespace TailwindVariants.NET.Tests;

public partial class ButtonComponent : ISlotted<ButtonSlots>
{
    public string? Class { get; set; }
    public ButtonSlots? Classes { get; set; }
    public bool IsDisabled { get; set; }
    public bool IsLoading { get; set; }
    public string? Size { get; set; }
    public string? Variant { get; set; }
}