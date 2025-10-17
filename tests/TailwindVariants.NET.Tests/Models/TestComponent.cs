namespace TailwindVariants.NET.Tests;

public class TestComponent : IStyleable
{
	public string? Class { get; set; }
	public string? Color { get; set; }
	public bool IsDisabled { get; set; }
	public string? Size { get; set; }
}
