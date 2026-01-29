namespace TailwindVariants.NET.Tests;

public class TwVariantsNullHandlingTests : BunitContext
{
	public TwVariantsNullHandlingTests() => Services.AddTailwindVariants();

	public static TheoryData<string?> NullOrEmptyBaseData { get; } = new()
	{
		{ null },
		{ "" }
	};

	private TwVariants Tv => Services.GetRequiredService<TwVariants>();

	[Theory]
	[MemberData(nameof(NullOrEmptyBaseData))]
	public void Base_NullOrEmpty_ReturnsNull(string? baseValue)
	{
		var descriptor = new TvDescriptor<TestComponent, TestSlots>(@base: baseValue);
		var component = new TestComponent();

		var result = Tv.Invoke(component, descriptor);

		Assert.Null(result[s => s.Base]);
	}

	[Theory]
	[MemberData(nameof(NullOrEmptyBaseData))]
	public void Invoke_WithNullOrEmptyClassesObject_HandlesGracefully(string? slotValue)
	{
		// Arrange
		var descriptor = new TvDescriptor<TestComponent, TestSlots>(
			@base: "component",
			slots: new()
			{
				[s => s.Title] = slotValue,
				[s => s.Description] = "text-lg"
			}
		);
		var component = new TestComponent { Classes = null };

		// Act
		var result = Tv.Invoke(component, descriptor);

		// Assert
		result.ShouldEqual(s => s.Base, "component");
		Assert.Null(result[s => s.Title]);
		result.ShouldEqual(s => s.Description, "text-lg");
	}

	[Theory]
	[MemberData(nameof(NullOrEmptyBaseData))]
	public void Invoke_WithNullOrEmptyStringClassOverride_AppendsEmpty(string? classValue)
	{
		// Arrange
		var descriptor = new TvDescriptor<TestComponent, TestSlots>(
			@base: "btn"
		);
		var component = new TestComponent { Class = classValue };

		// Act
		var tv = Services.GetRequiredService<TwVariants>();
		var result = tv.Invoke(component, descriptor);

		// Assert
		result.ShouldEqual(s => s.Base, "btn");
	}
}
