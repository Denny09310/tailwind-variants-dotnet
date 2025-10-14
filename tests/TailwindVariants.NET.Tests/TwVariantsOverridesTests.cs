using Tw = TailwindMerge.TwMerge;

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
		var component = new TestComponent
		{
			Classes = new TestSlots
			{
				Title = "font-bold",
				Description = null
			}
		};

		// Act
		var result = Tv.Invoke(component, descriptor);

		// Assert
		result.ContainsAll(s => s.Title,
			"text-lg",
			"font-bold");

		Assert.Null(result[s => s.Description]);
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
		var component = new TestComponent
		{
			Classes = new TestSlots
			{
				Title = "font-extrabold",
				Description = "italic"
			}
		};

		// Act
		var result = Tv.Invoke(component, descriptor);

		// Assert
		result.ContainsAll(s => s.Title,
			"text-lg",
			"font-extrabold");

		result.ContainsAll(s => s.Description,
			"text-sm",
			"italic");
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
		result.ContainsAll(s => s.Base,
			"btn",
			"bg-blue-500",
			"hover:bg-blue-600");
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
		result.ContainsAll(s => s.Base,
			"p-8",
			"bg-blue-500");

		result.DoesNotContainAny(s => s.Base,
			"p-4",
			"bg-red-500");
	}
}
