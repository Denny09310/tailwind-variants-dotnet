using Tw = TailwindMerge.TwMerge;

namespace TailwindVariants.NET.Tests;

public class TwVariantsNullHandlingTests
{
	private readonly TwVariants _tv = new(new Tw());

	[Fact]
	public void Invoke_WithNullBaseValue_ReturnsEmptyBaseSlot()
	{
		// Arrange
		var descriptor = new TvDescriptor<TestComponent, TestSlots>(
			@base: null
		);
		var component = new TestComponent();

		// Act
		var result = _tv.Invoke(component, descriptor);

		// Assert
		Assert.Null(result[s => s.Base]);
	}

	[Fact]
	public void Invoke_WithEmptyStringBase_ReturnsEmptyBaseSlot()
	{
		// Arrange
		var descriptor = new TvDescriptor<TestComponent, TestSlots>(
			@base: ""
		);
		var component = new TestComponent();

		// Act
		var result = _tv.Invoke(component, descriptor);

		// Assert
		Assert.Null(result[s => s.Base]);
	}

	[Fact]
	public void Invoke_WithNullClassOverride_IgnoresOverride()
	{
		// Arrange
		var descriptor = new TvDescriptor<TestComponent, TestSlots>(
			@base: "btn bg-blue-500"
		);
		var component = new TestComponent { Class = null };

		// Act
		var result = _tv.Invoke(component, descriptor);

		// Assert
		Assert.Equal("btn bg-blue-500", result[s => s.Base]);
	}

	[Fact]
	public void Invoke_WithEmptyStringClassOverride_AppendsEmpty()
	{
		// Arrange
		var descriptor = new TvDescriptor<TestComponent, TestSlots>(
			@base: "btn"
		);
		var component = new TestComponent { Class = "" };

		// Act
		var result = _tv.Invoke(component, descriptor);

		// Assert
		Assert.Equal("btn", result[s => s.Base]);
	}

	[Fact]
	public void Invoke_WithNullClassesObject_HandlesGracefully()
	{
		// Arrange
		var descriptor = new TvDescriptor<TestComponent, TestSlots>(
			@base: "component",
			slots: new()
			{
				[s => s.Title] = "text-lg"
			}
		);
		var component = new TestComponent { Classes = null };

		// Act
		var result = _tv.Invoke(component, descriptor);

		// Assert
		Assert.Equal("component", result[s => s.Base]);
		Assert.Equal("text-lg", result[s => s.Title]);
	}

	[Fact]
	public void Invoke_WithNullSlotValue_ReturnsEmptySlot()
	{
		// Arrange
		var descriptor = new TvDescriptor<TestComponent, TestSlots>(
			@base: "container",
			slots: new()
			{
				[s => s.Title] = null,
				[s => s.Description] = "text-sm"
			}
		);
		var component = new TestComponent();

		// Act
		var result = _tv.Invoke(component, descriptor);

		// Assert
		Assert.Equal("container", result[s => s.Base]);
		Assert.Null(result[s => s.Title]);
		Assert.Equal("text-sm", result[s => s.Description]);
	}
}
