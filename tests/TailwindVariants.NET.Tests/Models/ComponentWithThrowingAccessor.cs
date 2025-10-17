namespace TailwindVariants.NET.Tests;

public class ComponentWithThrowingAccessor : IStyleable
{
	public string? Class { get; set; }
	public TestSlots Classes { get; set; } = null!;
	public string? Size => throw new InvalidOperationException("test-throw");
}
