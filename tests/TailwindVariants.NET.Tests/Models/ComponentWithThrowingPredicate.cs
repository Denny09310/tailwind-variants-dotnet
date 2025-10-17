namespace TailwindVariants.NET.Tests;

public class ComponentWithThrowingPredicate : IStyleable
{
	public string? Class { get; set; }

	public bool PredicateThrows() => throw new InvalidOperationException("pred-throw");

	public TestSlots Classes { get; set; } = null!;
}
