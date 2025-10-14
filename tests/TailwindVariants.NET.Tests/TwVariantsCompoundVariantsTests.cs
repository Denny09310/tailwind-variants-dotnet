namespace TailwindVariants.NET.Tests;

public class TwVariantsCompoundVariantsTests : TestContext
{
	public TwVariantsCompoundVariantsTests() => Services.AddTailwindVariants();

	private TwVariants Tv => Services.GetRequiredService<TwVariants>();

	[Fact]
	public void Invoke_WithCompoundVariantEmptyClass_HandlesGracefully()
	{
		// Arrange
		var descriptor = new TvDescriptor<TestComponent, TestSlots>(
			@base: "btn",
			compoundVariants:
			[
				new(c => c.IsDisabled)
				{
					Class = ""
				}
			]
		);
		var component = new TestComponent { IsDisabled = true };

		// Act
		var result = Tv.Invoke(component, descriptor);

		// Assert
		result.ShouldEqual(b => b.Base, "btn");
	}

	[Fact]
	public void Invoke_WithCompoundVariantNullClass_OnlyAppliesSlots()
	{
		// Arrange
		var descriptor = new TvDescriptor<TestComponent, TestSlots>(
			@base: "btn",
			compoundVariants:
			[
				new(c => c.IsDisabled)
				{
					Class = null,
					[s => s.Title] = "text-gray-400"
				}
			],
			slots: new()
			{
				[s => s.Title] = "text-lg"
			}
		);
		var component = new TestComponent { IsDisabled = true };

		// Act
		var result = Tv.Invoke(component, descriptor);

		// Assert
		result.ShouldEqual(s => s.Base, "btn");
		result.ContainsAll(s => s.Title, "text-gray-400");
	}

	[Fact]
	public void Invoke_WithCompoundVariants_AppliesWhenPredicateMatches()
	{
		// Arrange
		var descriptor = new TvDescriptor<TestComponent, TestSlots>(
			@base: "btn",
			variants: new VariantCollection<TestComponent, TestSlots>
			{
				{
					c => c.Size,
					new Variant<string, TestSlots>
					{
						["sm"] = "text-sm",
						["lg"] = "text-lg"
					}
				}
			},
			compoundVariants:
			[
				new(c => c.Size == "lg" && c.IsDisabled)
				{
					Class = "opacity-50 cursor-not-allowed"
				}
			]
		);
		var component = new TestComponent { Size = "lg", IsDisabled = true };

		// Act
		var result = Tv.Invoke(component, descriptor);

		// Assert
		result.ContainsAll(s => s.Base,
			"opacity-50",
			"cursor-not-allowed");
	}

	[Fact]
	public void Invoke_WithCompoundVariants_DoesNotApplyWhenPredicateFails()
	{
		// Arrange
		var descriptor = new TvDescriptor<TestComponent, TestSlots>(
			@base: "btn",
			compoundVariants:
			[
				new(c => c.Size == "lg" && c.IsDisabled)
				{
					Class = "opacity-50"
				}
			]
		);
		var component = new TestComponent { Size = "sm", IsDisabled = true };

		// Act
		var tv = Services.GetRequiredService<TwVariants>();
		var result = tv.Invoke(component, descriptor);

		// Assert
		result.DoesNotContainAny(s => s.Base, "opacity-50");
	}

	[Fact]
	public void Invoke_WithCompoundVariantSlots_AppliesClassesToSpecificSlots()
	{
		// Arrange
		var descriptor = new TvDescriptor<TestComponent, TestSlots>(
			@base: "component",
			slots: new()
			{
				[s => s.Title] = "text-lg",
				[s => s.Description] = "text-sm"
			},
			compoundVariants:
			[
				new(c => c.IsDisabled)
				{
					[s => s.Title] = "text-gray-400",
					[s => s.Description] = "text-gray-300"
				}
			]
		);
		var component = new TestComponent { IsDisabled = true };

		// Act
		var result = Tv.Invoke(component, descriptor);

		// Assert
		result.ContainsAll(s => s.Title, "text-gray-400");
		result.ContainsAll(s => s.Description, "text-gray-300");
	}
}
