namespace TailwindVariants.NET.Tests;

public class TestComponent : ISlotted<TestSlots>
{
    public string? Class { get; set; }
    public TestSlots? Classes { get; set; }
    public string? Color { get; set; }
    public bool IsDisabled { get; set; }
    public string? Size { get; set; }
}