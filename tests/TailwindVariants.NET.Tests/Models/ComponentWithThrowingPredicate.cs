namespace TailwindVariants.NET.Tests;

public class ComponentWithThrowingPredicate
{
	public string? Class { get; set; }

	public bool PredicateThrows() => throw new InvalidOperationException("pred-throw");

	public TestSlots Classes { get; set; } = null!;
}
