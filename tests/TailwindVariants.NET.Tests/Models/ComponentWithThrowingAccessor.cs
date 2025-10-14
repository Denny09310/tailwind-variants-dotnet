namespace TailwindVariants.NET.Tests;

public class ComponentWithThrowingAccessor : ISlotted<TestSlots>
{
	public string? Class { get; set; }
	public TestSlots Classes { get; set; } = null!;
	public string? Size => throw new InvalidOperationException("test-throw");
}
