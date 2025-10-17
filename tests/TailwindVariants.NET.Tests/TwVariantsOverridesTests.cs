namespace TailwindVariants.NET.Tests;

public class TwVariantsOverridesTests : TestContext
{
	public TwVariantsOverridesTests() => Services.AddTailwindVariants();

	private TwVariants Tv => Services.GetRequiredService<TwVariants>();

	[Fact]
	public void Invoke_WithClassesContainingNullSlot_SkipsNullSlots()
	{
		// Arrange
		var descriptor = new TvDescriptor<TestComponent, TestSlots>(
			@base: "component",
			slots: new()
			{
				[s => s.Title] = "text-lg"
			}
		);
		var component = new TestComponent();

		// Act
		var result = Tv.Invoke(component, descriptor);

		// Assert
		result.ContainsAll(s => s.Title, classes: "font-bold", expectedTokens: [
			"text-lg",
			"font-bold"]);

		Assert.Null(result[s => s.Description](default));
	}

	[Fact]
	public void Invoke_WithClassesOverride_AppendsToSpecificSlots()
	{
		// Arrange
		var descriptor = new TvDescriptor<TestComponent, TestSlots>(
			@base: "component",
			slots: new()
			{
				[s => s.Title] = "text-lg",
				[s => s.Description] = "text-sm"
			}
		);
		var component = new TestComponent();

		// Act
		var result = Tv.Invoke(component, descriptor);

		// Assert
		result.ContainsAll(s => s.Title, classes: "font-extrabold", expectedTokens: [
			"text-lg",
			"font-extrabold"]);

		result.ContainsAll(s => s.Description, classes: "italic", expectedTokens: [
			"text-sm",
			"italic"]);
	}

	[Fact]
	public void Invoke_WithClassOverride_AppendsToBaseSlot()
	{
		// Arrange
		var descriptor = new TvDescriptor<TestComponent, TestSlots>(
			@base: "btn bg-blue-500"
		);
		var component = new TestComponent { Class = "hover:bg-blue-600" };

		// Act
		var result = Tv.Invoke(component, descriptor);

		// Assert
		result.ContainsAll(s => s.Base, component.Class, expectedTokens: [
			"btn",
			"bg-blue-500",
			"hover:bg-blue-600"]);
	}

	[Fact]
	public void Invoke_WithTailwindMerge_ResolvesConflictingClasses()
	{
		// Arrange
		var descriptor = new TvDescriptor<TestComponent, TestSlots>(
			@base: "p-4 bg-red-500"
		);
		var component = new TestComponent { Class = "p-8 bg-blue-500" };

		// Act
		var result = Tv.Invoke(component, descriptor);

		// Assert
		result.ContainsAll(s => s.Base, component.Class, expectedTokens: [
			"p-8",
			"bg-blue-500"]);

		result.DoesNotContainAny(s => s.Base, component.Class, expectedTokens: [
			"p-4",
			"bg-red-500"]);
	}
}
